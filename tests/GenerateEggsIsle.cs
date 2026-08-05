using Godot;
using System;

/// <summary>
/// One-shot headless helper that generates the Eggs Isle arrival/intake level
/// (res://levels/eggsile/maps/EggsIsle.tscn) using Godot's own scene serialization
/// (so tile_map_data bytes are always correct). Replaces the bare area1.tscn as the
/// factory→eggsile destination per issue #130.
///
///   Dock/processing (west) → cell block (center) → kitchen gate (east)
///
/// Run with: godot --headless --path . --script res://tests/GenerateEggsIsle.cs
/// </summary>
public partial class GenerateEggsIsle : SceneTree
{
    private const string Tileset = "res://assets/tilemaps/eggsile_tileset.tres";
    private const string BaseLevelScript = "res://levels/BaseLevel.cs";
    private const string LayerScript = "res://components/core/LevelTileMapLayer.cs";
    private const string TransitionScene = "res://levels/LevelTransition.tscn";
    private const string SaveScene = "res://saves/SavePoint.tscn";
    private const string WarpScene = "res://components/maps/WarpPoint.tscn";
    private const string CutsceneTriggerScene = "res://components/npcs/CutsceneTrigger.tscn";
    private const string PickupScript = "res://components/items/PickupItem.cs";
    private const string IntakeTowelsScript = "res://components/eggsile/IntakeTowels.cs";
    private const string JoeScene = "res://levels/eggsile/npcs/Joe.tscn";
    private const string FrankScene = "res://levels/eggsile/npcs/Frank.tscn";
    private const string BaconScene = "res://levels/factory/npcs/FactoryOfficerBacon.tscn";
    private const string ReadableScene = "res://components/npcs/ReadableObject.tscn";
    private const string CellKeySprite = "res://assets/items/sprites/item_sprite_0009.png";
    private const string Music = "res://assets/audio/music/indie_meditations/lvl_3_the_grassland.ogg";
    private const string Ambience = "res://assets/audio/music/generated/isle_tide_ambient.ogg";

    // Valid tiles in eggsile_tileset.tres (prison tileset.png atlas).
    private static readonly Vector2I Floor = new(2, 2);     // dominant stone floor (proven in area1)
    private static readonly Vector2I Accent = new(3, 4);    // accent patch (proven in area1)
    private static readonly Vector2I Accent2 = new(4, 4);   // second accent (proven in area1)

    private Node _liveRoot;

    public override void _Initialize()
    {
        GD.Print("[generate-eggsisle] Building EggsIsle.tscn ...");
        _liveRoot = new Node { Name = "EggsIsleGenBuilder" };
        Root.AddChild(_liveRoot);

        Build();

        GD.Print("[generate-eggsisle] DONE — saved res://levels/eggsile/maps/EggsIsle.tscn");
        System.Environment.Exit(0);
    }

    private void Build()
    {
        var root = MakeLevel("EggsIsle", "Eggs Isle — Intake");
        var tiles = AddTiles(root, 110, 70, Floor); // x -880..880, y -560..560 (1760×1120 px)

        // --- Dock / processing accents (west, near arrival) ---
        // Dock strip along the west edge.
        for (int y = -24; y <= 26; y++)
            for (int x = -53; x <= -45; x++)
                tiles.SetCell(new Vector2I(x, y), 0, Accent2, 0);

        // --- Cell block accents (center) ---
        for (int y = -18; y <= 14; y++)
            for (int x = -10; x <= 28; x++)
                if ((x + y) % 4 == 0)
                    tiles.SetCell(new Vector2I(x, y), 0, Accent, 0);

        // --- Kitchen hall accents (east, near the gate) ---
        for (int y = -8; y <= 8; y++)
            for (int x = 34; x <= 44; x++)
                tiles.SetCell(new Vector2I(x, y), 0, Accent2, 0);

        // --- Arrival / save / warp (same positions as area1 → stable spawns) ---
        // HubArrival is the arrest-handoff + warp spawn anchor. The Overworld hub it
        // used to lead back to was removed in #174, so it self-anchors (no-op return).
        AddTransition(root, "HubArrival", new Vector2(-640, 320), TransitionSide.Left, 4,
            "res://levels/eggsile/maps/EggsIsle.tscn", "HubArrival", "");
        AddSave(root, "HubSavePoint", "EggsIsle Hub", new Vector2(-576, 320));
        AddWarp(root, "eggsile_area1", new Vector2(0, -30));

        // --- Intro cutscene: one-shot OnEnter at the dock (overlaps factory spawn) ---
        AddCutsceneTrigger(root, "ArrivalCutscene", new Vector2(-640, 256), TriggerMode.OnEnter,
            once: true, cutsceneId: "eggsile_arrival", cutscene: "res://levels/eggsile/npcs/EggsIsleArrivalCutscene.tres",
            setFlags: null, dialog: null, shapeSize: new Vector2(140, 80));

        // --- Intake NPCs ---
        // Joe at the processing desk (scene sets met_joe on its trigger).
        InstantiateScene(root, JoeScene, "Joe", new Vector2(-87, -83));

        // Officer Bacon at the dock for the handoff cutscene; repeatable line after.
        // (Chat trigger is a direct-root node — deep-child overrides don't survive Pack().)
        InstantiateScene(root, BaconScene, "OfficerBacon", new Vector2(-500, 300));
        AddCutsceneTrigger(root, "BaconChat", new Vector2(-500, 300), TriggerMode.OnInteract,
            once: false, cutsceneId: "", cutscene: null, setFlags: null,
            dialog: new[] { "Officer Bacon: Try to keep your shell intact while you're here, Eggbert." },
            shapeSize: new Vector2(90, 90));

        // History book (readable → read_history_book per #75).
        var book = InstantiateScene(root, ReadableScene, "HistoryBook", new Vector2(-140, -120));
        book.Set("GateFlag", "history_book");
        book.Set("Once", true);
        book.Set("DialogLines", new[] {
            "\"Eggs Isle: A History\" — chapter one: the island was built to hold the continent's most dangerous breakfast offenders.",
            "Chapter two: the warden's predecessor once tried to ban breakfast itself. The inmates rioted for three days.",
            "Chapter three: the Sunnysides began as a small prayer group in Block B. Now they run the rumor mill. Nobody will confirm or deny.",
            "A marginal note in pen: \"The courtyard quiz rewards careful readers. Just saying.\""
        });

        // Frank in the cell block (scene sets met_frank + conditional handoff via FrankIntake.tres).
        InstantiateScene(root, FrankScene, "Frank", new Vector2(200, -80));

        // --- Scavenger: 3 towels + tracker (kept from area1) ---
        AddCutsceneTrigger(root, "Towel1", new Vector2(120, -180), TriggerMode.OnEnter,
            once: true, cutsceneId: "towel_1", cutscene: null, setFlags: null,
            dialog: new[] { "Found a towel. Somehow both damp and dusty." }, shapeSize: new Vector2(40, 40));
        AddCutsceneTrigger(root, "Towel2", new Vector2(300, -120), TriggerMode.OnEnter,
            once: true, cutsceneId: "towel_2", cutscene: null, setFlags: null,
            dialog: new[] { "Found a towel. Still damp, still dusty." }, shapeSize: new Vector2(40, 40));
        AddCutsceneTrigger(root, "Towel3", new Vector2(-260, -160), TriggerMode.OnEnter,
            once: true, cutsceneId: "towel_3", cutscene: null, setFlags: null,
            dialog: new[] { "Found a clean towel. They had one this whole time." }, shapeSize: new Vector2(40, 40));

        var tracker = new Node { Name = "IntakeTowels" };
        root.AddChild(tracker);
        tracker.SetScript(GD.Load<Script>(IntakeTowelsScript));
        var liveTracker = root.GetNode<Node>("IntakeTowels");
        liveTracker.Owner = root;

        // --- Reward: cell key (kept from area1 + found_cell_key per #75) ---
        AddPickup(root, "CellKeyPickup", "cell_key", new Vector2(500, -200),
            new[] { "Found a Cell Key! This must be what Frank was talking about." },
            new[] { "found_cell_key" });

        // --- Outgoing transitions (all direct-root) ---
        // Exits to the Kitchen, Sewers, Overworld hub, and Sandbox were removed in the
        // 2026-08 test-content cleanup (#174) — the intake is the end of the tutorial
        // chain; fast travel (eggsile_area1 warp) is the only way back to the factory.

        Save(root, "res://levels/eggsile/maps/EggsIsle.tscn");
    }

    // -----------------------------------------------------------------------
    // Builders
    // -----------------------------------------------------------------------
    private Node2D MakeLevel(string name, string levelName)
    {
        var root = new Node2D { Name = name };
        _liveRoot.AddChild(root);
        root.SetScript(GD.Load<Script>(BaseLevelScript));
        var live = _liveRoot.GetNode<Node2D>(name);
        live.Set("LevelName", levelName);
        live.Set("LevelMusic", GD.Load<AudioStream>(Music));
        live.Set("LevelAmbience", GD.Load<AudioStream>(Ambience));
        return live;
    }

    private TileMapLayer AddTiles(Node2D root, int width, int height, Vector2I atlas)
    {
        var layer = new TileMapLayer { Name = "CoreTilemapLayer" };
        root.AddChild(layer);
        layer.SetScript(GD.Load<Script>(LayerScript));
        var live = root.GetNode<TileMapLayer>("CoreTilemapLayer");
        live.TileSet = GD.Load<TileSet>(Tileset);
        live.Owner = root;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                live.SetCell(new Vector2I(x - width / 2, y - height / 2), 0, atlas, 0);
        return live;
    }

    private static void AddTransition(Node2D root, string name, Vector2 pos, TransitionSide side, int size,
        string level, string target, string requiredFlag, string[] setFlags = null)
    {
        var t = GD.Load<PackedScene>(TransitionScene).Instantiate<LevelTransition>();
        root.AddChild(t);
        t.Owner = root;
        t.Name = name;
        t.Position = pos;
        t.Side = side;
        t.Size = size;
        t.Level = level;
        t.TargetTransitionName = target;
        t.RequiredFlag = requiredFlag;
        t.SetFlagsOnFire = setFlags;
    }

    private static void AddSave(Node2D root, string name, string location, Vector2 pos)
    {
        var save = GD.Load<PackedScene>(SaveScene).Instantiate<Area2D>();
        root.AddChild(save);
        save.Owner = root;
        save.Name = name;
        save.Position = pos;
        save.Set("LocationName", location);
    }

    private static void AddWarp(Node2D root, string warpId, Vector2 pos)
    {
        var warp = GD.Load<PackedScene>(WarpScene).Instantiate<Area2D>();
        root.AddChild(warp);
        warp.Owner = root;
        warp.Position = pos;
        warp.Set("WarpId", warpId);
    }

    private static void AddCutsceneTrigger(Node2D root, string name, Vector2 pos, TriggerMode mode,
        bool once, string cutsceneId, string cutscene, string[] setFlags, string[] dialog, Vector2 shapeSize)
    {
        var trigger = GD.Load<PackedScene>(CutsceneTriggerScene).Instantiate<Area2D>();
        root.AddChild(trigger);
        trigger.Owner = root;
        trigger.Name = name;
        trigger.Position = pos;
        trigger.Set("Mode", (int)mode);
        trigger.Set("Once", once);
        trigger.Set("CutsceneId", cutsceneId);
        if (!string.IsNullOrEmpty(cutscene))
            trigger.Set("Cutscene", GD.Load<Resource>(cutscene));
        if (setFlags != null)
            trigger.Set("SetFlagsOnFire", setFlags);
        if (dialog != null)
            trigger.Set("DialogLines", dialog);

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = shapeSize }
        };
        trigger.AddChild(shape);
        shape.Owner = root;
    }

    private static void AddPickup(Node2D root, string name, string itemId, Vector2 pos, string[] dialog, string[] setFlags)
    {
        var area = new Area2D { Name = name, Position = pos };
        root.AddChild(area);
        area.SetScript(GD.Load<Script>(PickupScript));
        var liveArea = root.GetNode<Area2D>(name);
        liveArea.Owner = root;

        var sprite = new Sprite2D();
        sprite.Texture = GD.Load<Texture2D>(CellKeySprite);
        liveArea.AddChild(sprite);
        sprite.Owner = root;

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(24, 24) }
        };
        liveArea.AddChild(shape);
        shape.Owner = root;

        liveArea.Set("ItemId", itemId);
        liveArea.Set("Count", 1);
        liveArea.Set("DialogLines", dialog);
        liveArea.Set("SetFlag", setFlags);
    }

    private static Node2D InstantiateScene(Node2D root, string scenePath, string name, Vector2 pos)
    {
        var node = GD.Load<PackedScene>(scenePath).Instantiate<Node2D>();
        root.AddChild(node);
        node.Owner = root;
        node.Name = name;
        node.Position = pos;
        // Re-fetch from the tree — the pre-AddChild C# wrapper can be collected (Godot mono GC).
        return root.GetNode<Node2D>(name);
    }

    private static void Save(Node2D root, string path)
    {
        var packed = new PackedScene();
        packed.Pack(root);
        var err = ResourceSaver.Save(packed, path);
        GD.Print($"[generate-eggsisle] Saved '{path}' -> {err}");
        root.Free();
    }
}
