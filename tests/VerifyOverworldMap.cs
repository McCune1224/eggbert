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
        // Factory opening: 2 transitions (HubArrival + SortingFloorEntrance), no NPC
        // nodes (TimeClock/VendingMachine are props). Overworld/Zone1 exits removed in #174.
        ("res://levels/factory/maps/OpeningZone.tscn", 2, 0, 0, 1, 1),
        // Sorting floor: Jamitor NPC + 2 transitions.
        ("res://levels/factory/maps/SortingFloor.tscn", 2, 1, 4, 0, 0),
        // Eggs Isle intake: Joe, OfficerBacon (npc group), Frank + the HubArrival anchor.
        ("res://levels/eggsile/maps/EggsIsle.tscn", 1, 3, 6, 1, 1),
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
        await VerifyComposedMap();

        GD.Print(_failures == 0
            ? $"[overworld-map] ALL OK — {_passes} checks passed"
            : $"[overworld-map] {_failures} FAILURE(S)");
        Quit(_failures == 0 ? 0 : 1);
    }

    /// <summary>
    /// Composes the pause-menu full map for OpeningZone and asserts the baked-in pixels:
    /// frame ring, door marker color, player dot, and facing nub. Also checks the
    /// OverworldMenu Map-tab node layout.
    /// </summary>
    private async System.Threading.Tasks.Task VerifyComposedMap()
    {
        const string path = "res://levels/factory/maps/OpeningZone.tscn";
        var scene = ResourceLoader.Load<PackedScene>(path);
        var level = scene.Instantiate();
        Root.AddChild(level);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        var data = LevelMapGenerator.Generate(level);
        Check(data != null, "compose: OpeningZone map generates");
        if (data != null)
        {
            Vector2 playerPos = data.WorldBounds.GetCenter();
            Vector2? questPos = data.WorldBounds.GetCenter() + new Vector2(0, -64);
            var composed = LevelMapGenerator.ComposeMapTexture(data, playerPos, questPos, Vector2.Down);
            Check(composed != null, "compose: texture produced");
            var img = composed.GetImage();
            Check(img.GetWidth() == data.Texture.GetWidth() && img.GetHeight() == data.Texture.GetHeight(),
                "compose: texture matches base size");

            // Frame: inner cream ring at (0,0).
            Check(SamePixel(img.GetPixel(0, 0), MapPixelArt.FrameLight), "compose: frame ring pixel present");

            // Player dot: cream diamond at the world-bounds center, with a black nub below (facing Down).
            // The nub overwrites the diamond's (0,+2) pixel — (0,+3) is covered by the outline either way.
            Vector2 pp = data.WorldToMap(playerPos).Floor();
            Check(SamePixel(img.GetPixel((int)pp.X, (int)pp.Y), MapPixelArt.PlayerDot),
                "compose: player dot pixel at its position");
            Check(SamePixel(img.GetPixel((int)pp.X, (int)pp.Y + 2), MapPixelArt.PlayerDotOutline),
                "compose: facing nub points down");

            // Quest star at the quest position.
            Vector2 qp = data.WorldToMap(questPos.Value).Floor();
            Check(SamePixel(img.GetPixel((int)qp.X, (int)qp.Y), MapPixelArt.QuestDot),
                "compose: quest star pixel at its position");

            // Every static marker's center pixel matches its kind color — skipping markers
            // that sit under the player/quest dots (drawn later, they win the pixel).
            bool markersOk = true;
            foreach (MapMarker marker in data.Markers)
            {
                if (!data.ContainsWorld(marker.WorldPosition))
                    continue;
                Vector2 mp = data.WorldToMap(marker.WorldPosition).Floor();
                if (mp.DistanceTo(pp) <= 4f || mp.DistanceTo(qp) <= 3f)
                    continue;
                if (!SamePixel(img.GetPixel((int)mp.X, (int)mp.Y), MapPixelArt.KindColor(marker.Kind)))
                    markersOk = false;
            }
            Check(markersOk, "compose: all marker centers use their kind colors");
        }

        level.QueueFree();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        // Pause-menu Map tab layout (no AddChild — _Ready side effects not needed).
        var menuScene = ResourceLoader.Load<PackedScene>("res://ui/OverworldMenu.tscn");
        if (menuScene == null)
        {
            Fail("compose: OverworldMenu.tscn failed to load");
            return;
        }
        var menu = menuScene.Instantiate();
        Check(menu.GetNodeOrNull("MapPanel/VBoxContainer/MapTexture") is TextureRect,
            "compose: MapTexture present in Map tab");
        Check(menu.GetNodeOrNull("MapPanel/VBoxContainer/MapTitleLabel") is Label,
            "compose: MapTitleLabel present in Map tab");
        Check(menu.GetNodeOrNull("MapPanel/VBoxContainer/MapObjectiveLabel") is Label,
            "compose: MapObjectiveLabel present in Map tab");
        Check(menu.GetNodeOrNull("MapPanel/VBoxContainer/WarpGrid") is GridContainer,
            "compose: WarpGrid still present in Map tab");
        menu.Free();
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
        // Door nodes may legitimately sit outside the painted rect (e.g. spawn-anchor
        // arrivals); the HUD clamps their dots to the map edge. Fail only on truly
        // absurd placement (> half the map's diagonal beyond the bounds).
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
            Check(intakeQuest.Objectives.Count == 4, "EggsIsleIntakeQuest has 4 objectives");
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

    /// <summary>Pixel equality with tolerance for Rgba8 quantization (1/255 per channel).</summary>
    private static bool SamePixel(Color a, Color b)
    {
        return Mathf.Abs(a.R - b.R) < 0.01f
            && Mathf.Abs(a.G - b.G) < 0.01f
            && Mathf.Abs(a.B - b.B) < 0.01f
            && Mathf.Abs(a.A - b.A) < 0.01f;
    }

    private void Fail(string msg)
    {
        _failures++;
        GD.Print("[overworld-map] FAIL: " + msg);
    }
}
