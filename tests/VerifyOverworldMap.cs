using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Headless verifier for the overworld HUD map (issue #170).
/// For a set of representative levels: instantiates the scene, generates the
/// schematic map via <see cref="LevelMapGenerator"/>, and asserts the texture
/// dimensions, world→map projection, and marker detection (doors, NPCs, and that
/// trigger-rooted props are NOT flagged as NPCs). Run with:
///   godot --headless --path . --script res://tests/VerifyOverworldMap.cs
/// </summary>
public partial class VerifyOverworldMap : SceneTree
{
    private int _failures;
    private int _passes;

    private readonly List<(string Path, int MinDoors, int MinNpcs, int MaxNpcs, int MinSaves, int MinWarps)> _levels = new()
    {
        // Factory opening: 4+ transitions, no NPC nodes (TimeClock/VendingMachine are props).
        ("res://levels/factory/maps/OpeningZone.tscn", 4, 0, 0, 1, 1),
        // Sorting floor: Jamitor NPC + 2 transitions.
        ("res://levels/factory/maps/SortingFloor.tscn", 2, 1, 4, 0, 0),
        // Eggs Isle intake: Joe, OfficerBacon (npc group), Frank + exits.
        ("res://levels/eggsile/maps/EggsIsle.tscn", 2, 3, 6, 1, 1),
        // NOTE: Overworld.tscn is excluded — its tileset (overworld_tileset.tres) fails
        // to load (atlas tiles defined outside the texture, same class as #128), so the
        // layer has a null TileSet and no map can be generated. Tracked in issue #171.
        // Sandbox hub: generated, 3 transitions, no NPCs.
        ("res://levels/sandbox/maps/SandboxHub.tscn", 3, 0, 0, 1, 1),
    };

    public override async void _Initialize()
    {
        await ToSignal(Root, Window.SignalName.Ready);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        foreach (var (path, minDoors, minNpcs, maxNpcs, minSaves, minWarps) in _levels)
        {
            await CheckLevel(path, minDoors, minNpcs, maxNpcs, minSaves, minWarps);
        }

        VerifyQuestMarkers();

        GD.Print(_failures == 0
            ? $"[overworld-map] ALL OK — {_passes} checks passed"
            : $"[overworld-map] {_failures} FAILURE(S)");
        Quit(_failures == 0 ? 0 : 1);
    }

    private async System.Threading.Tasks.Task CheckLevel(string path, int minDoors, int minNpcs, int maxNpcs,
        int minSaves, int minWarps)
    {
        string tag = path.GetFile();
        if (!FileAccess.FileExists(path))
        {
            Fail($"{tag}: scene file missing");
            return;
        }
        var scene = ResourceLoader.Load<PackedScene>(path);
        if (scene == null)
        {
            Fail($"{tag}: scene failed to load");
            return;
        }

        Node level = scene.Instantiate();
        Root.AddChild(level);
        // Let _Ready run (MapBorders spawn, bounds register) and physics settle.
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        LevelMapData data = LevelMapGenerator.Generate(level);
        sw.Stop();

        if (data == null)
        {
            Fail($"{tag}: Generate returned null (no usable tilemap)");
            level.QueueFree();
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
            return;
        }

        int doors = 0, npcs = 0, saves = 0, warps = 0;
        foreach (MapMarker marker in data.Markers)
        {
            switch (marker.Kind)
            {
                case MapMarkerKind.Door: doors++; break;
                case MapMarkerKind.Npc: npcs++; break;
                case MapMarkerKind.SavePoint: saves++; break;
                case MapMarkerKind.WarpPoint: warps++; break;
            }
        }

        Pass($"{tag}: map {data.Texture.GetWidth()}x{data.Texture.GetHeight()}px over " +
             $"{data.Cells.X}x{data.Cells.Y} cells (ppc={data.PixelsPerCell:F2}, {sw.ElapsedMilliseconds}ms)");

        Check(data.Texture != null, $"{tag}: texture present");
        Check(data.Texture.GetWidth() >= 4 && data.Texture.GetHeight() >= 4,
            $"{tag}: texture non-trivial size");
        Check(data.Texture.GetWidth() <= LevelMapGenerator.MaxMapWidth
              && data.Texture.GetHeight() <= LevelMapGenerator.MaxMapHeight,
            $"{tag}: texture within display cap");
        Check(data.WorldBounds.Size.X > 0 && data.WorldBounds.Size.Y > 0,
            $"{tag}: world bounds non-empty");
        Check(data.PixelsPerCell > 0f, $"{tag}: pixels-per-cell positive");

        // Projection sanity: bounds corners map to texture corners.
        Vector2 mapTopLeft = data.WorldToMap(data.WorldBounds.Position);
        Vector2 mapBottomRight = data.WorldToMap(data.WorldBounds.End);
        Check(mapTopLeft.DistanceTo(Vector2.Zero) < 0.5f, $"{tag}: bounds top-left maps to (0,0)");
        Check(Mathf.Abs(mapBottomRight.X - data.Texture.GetWidth()) <= 1f
              && Mathf.Abs(mapBottomRight.Y - data.Texture.GetHeight()) <= 1f,
            $"{tag}: bounds bottom-right maps to texture corner");

        Check(doors >= minDoors, $"{tag}: {doors} door markers (min {minDoors})");
        Check(npcs >= minNpcs && npcs <= maxNpcs, $"{tag}: {npcs} NPC markers (expected {minNpcs}..{maxNpcs})");
        Check(saves >= minSaves, $"{tag}: {saves} save-point markers (min {minSaves})");
        Check(warps >= minWarps, $"{tag}: {warps} warp markers (min {minWarps})");

        bool allMarkersInside = true;
        // Door nodes may legitimately sit far outside the painted rect (sandbox hub
        // exits are ~200px away); the HUD clamps their dots to the map edge. Fail only
        // on truly absurd placement (> half the map's diagonal beyond the bounds).
        float slack = Mathf.Max(LevelMapData.TileSize * 4f, data.WorldBounds.Size.Length() * 0.5f);
        Rect2 relaxed = data.WorldBounds.Grow(slack);
        foreach (MapMarker marker in data.Markers)
        {
            if (!relaxed.HasPoint(marker.WorldPosition))
            {
                allMarkersInside = false;
                Fail($"{tag}: marker '{marker.Label}' {marker.WorldPosition.DistanceTo(data.WorldBounds.GetCenter()):F0}px from bounds center");
            }
        }
        if (allMarkersInside)
            Pass($"{tag}: all markers project inside bounds");

        GD.Print($"[overworld-map] {tag}: doors=[{string.Join(", ", DoorLabels(data))}]");
        GD.Print($"[overworld-map] {tag}: npcs=[{string.Join(", ", NpcLabels(data))}]");

        level.QueueFree();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
    }

    private void VerifyQuestMarkers()
    {
        // The .tres quest files must load with the new LocationLevel/LocationPosition fields.
        var factoryQuest = GD.Load<QuestDefinition>("res://resources/quests/FactoryGateQuest.tres");
        Check(factoryQuest != null, "FactoryGateQuest.tres loads");
        if (factoryQuest != null)
        {
            bool allLocated = true;
            foreach (QuestObjective objective in factoryQuest.Objectives)
                if (string.IsNullOrEmpty(objective.LocationLevel)) allLocated = false;
            Check(factoryQuest.Objectives.Count == 4, "FactoryGateQuest has 4 objectives");
            Check(allLocated, "FactoryGateQuest objectives all have LocationLevel");
            Check(factoryQuest.Objectives[0].LocationLevel == "res://levels/factory/maps/OpeningZone.tscn"
                  && factoryQuest.Objectives[0].LocationPosition.IsEqualApprox(new Vector2(160, -64)),
                "clock_out objective location is the TimeClock spot");
        }

        var intakeQuest = GD.Load<QuestDefinition>("res://resources/quests/EggsIsleIntakeQuest.tres");
        Check(intakeQuest != null, "EggsIsleIntakeQuest.tres loads");
        if (intakeQuest != null)
        {
            bool allLocated = true;
            foreach (QuestObjective objective in intakeQuest.Objectives)
                if (string.IsNullOrEmpty(objective.LocationLevel)) allLocated = false;
            Check(intakeQuest.Objectives.Count == 5, "EggsIsleIntakeQuest has 5 objectives");
            Check(allLocated, "EggsIsleIntakeQuest objectives all have LocationLevel");
            Check(intakeQuest.Objectives[0].LocationLevel == "res://levels/eggsile/maps/EggsIsle.tscn"
                  && intakeQuest.Objectives[0].LocationPosition.IsEqualApprox(new Vector2(-87, -83)),
                "meet_joe objective location is Joe's spot");
        }

        // Resolver against the real pinned quest chain (FactoryGateQuest is always
        // active — StartFlag empty — and clock_out is its first objective).
        WorldFlags.Instance.SetFlag("quest_pinned_id", "factory_gate_shift_end");

        var resolved = LevelMapGenerator.ResolvePinnedObjectivePosition(
            QuestManager.Instance, "res://levels/factory/maps/OpeningZone.tscn");
        Check(resolved is Vector2 rv && rv.IsEqualApprox(new Vector2(160, -64)),
            "resolver returns clock_out position in OpeningZone");

        var otherLevel = LevelMapGenerator.ResolvePinnedObjectivePosition(
            QuestManager.Instance, "res://levels/eggsile/maps/EggsIsle.tscn");
        Check(otherLevel == null, "resolver returns null when the objective is in another level");

        // Complete clock_out → the next objective (talk_to_jamitor) lives in SortingFloor.
        WorldFlags.Instance.SetFlag("tutorial_clocked_out", true);
        var oldLevel = LevelMapGenerator.ResolvePinnedObjectivePosition(
            QuestManager.Instance, "res://levels/factory/maps/OpeningZone.tscn");
        Check(oldLevel == null, "resolver returns null in the old level after the objective advances");

        var nextLevel = LevelMapGenerator.ResolvePinnedObjectivePosition(
            QuestManager.Instance, "res://levels/factory/maps/SortingFloor.tscn");
        Check(nextLevel is Vector2 nv && nv.IsEqualApprox(new Vector2(-384, 0)),
            "resolver follows the objective into its next level (Jamitor spot)");
    }

    private static List<string> DoorLabels(LevelMapData data)
    {
        var labels = new List<string>();
        foreach (MapMarker m in data.Markers)
            if (m.Kind == MapMarkerKind.Door) labels.Add(m.Label);
        return labels;
    }

    private static List<string> NpcLabels(LevelMapData data)
    {
        var labels = new List<string>();
        foreach (MapMarker m in data.Markers)
            if (m.Kind == MapMarkerKind.Npc) labels.Add(m.Label);
        return labels;
    }

    private void Pass(string msg)
    {
        _passes++;
        GD.Print("[overworld-map] PASS: " + msg);
    }

    private void Check(bool condition, string msg)
    {
        if (condition) Pass(msg);
        else Fail(msg);
    }

    private void Fail(string msg)
    {
        _failures++;
        GD.Print("[overworld-map] FAIL: " + msg);
    }
}
