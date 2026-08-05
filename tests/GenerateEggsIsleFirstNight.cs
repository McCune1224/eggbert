using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// One-shot headless helper that builds the BRAND-NEW Eggs Isle exile ("First Night")
/// as three chained maps per docs/eggsile-first-night.md (issues #175–#181):
///
///   1. EggsIsle.tscn          — The Dock (moonlit pier, arrival cutscene, warp)
///   2. EggsIsleGatehouse.tscn — The Gatehouse (Mr Tea check-in ritual)
///   3. EggsIsleBlock.tscn     — The Overflow wing (cell + Frank, boilers, Tank,
///                               Gallery, nine o'clock count, tunnel hatch payoff)
///
/// All cinematics use AnimationPlayer cutscene scenes (components/cutscene/).
/// Run with: godot --headless --path . --script res://tests/GenerateEggsIsleFirstNight.cs
/// </summary>
public partial class GenerateEggsIsleFirstNight : SceneTree
{
    private const string Tileset = "res://assets/tilemaps/eggsile_tileset.tres";
    private const string BaseLevelScript = "res://levels/BaseLevel.cs";
    private const string LayerScript = "res://components/core/LevelTileMapLayer.cs";
    private const string TransitionScene = "res://levels/LevelTransition.tscn";
    private const string SaveScene = "res://saves/SavePoint.tscn";
    private const string WarpScene = "res://components/maps/WarpPoint.tscn";
    private const string CutsceneTriggerScene = "res://components/npcs/CutsceneTrigger.tscn";
    private const string ReadableScene = "res://components/npcs/ReadableObject.tscn";
    private const string PickupScript = "res://components/items/PickupItem.cs";
    private const string DoorScene = "res://components/puzzles/Door.tscn";
    private const string KeyDoorScene = "res://components/puzzles/KeyDoor.tscn";
    private const string PlateScene = "res://components/puzzles/WeightedPressurePlate.tscn";
    private const string CrateScene = "res://components/puzzles/PushBlock.tscn";

    private const string BaconScene = "res://levels/factory/npcs/FactoryOfficerBacon.tscn";
    private const string FrankScene = "res://levels/eggsile/npcs/Frank.tscn";
    private const string MrTeaScene = "res://levels/eggsile/npcs/MrTea.tscn";
    private const string MikanScene = "res://levels/eggsile/npcs/Mikan.tscn";
    private const string ScramblesScene = "res://levels/eggsile/npcs/Scrambles.tscn";
    private const string WienerCopScene = "res://levels/eggsile/npcs/WienerCop.tscn";

    private const string DockArrivalCutscene = "res://levels/eggsile/cutscenes/DockArrival.tscn";
    private const string CheckInCutscene = "res://levels/eggsile/cutscenes/GatehouseCheckIn.tscn";
    private const string PlacementCutscene = "res://levels/eggsile/cutscenes/CellPlacement.tscn";
    private const string CountCutscene = "res://levels/eggsile/cutscenes/CountEvent.tscn";

    private const string CellKeySprite = "res://assets/items/sprites/item_sprite_0009.png";
    private const string HardboiledSprite = "res://assets/items/sprites/item_sprite_0010.png";
    private const string DeviledSprite = "res://assets/items/sprites/item_sprite_0011.png";
    private const string YolkSprite = "res://assets/items/sprites/item_sprite_0012.png";

    private const string Music = "res://assets/audio/music/indie_meditations/lvl_5_the_oasis_or_resting_place.ogg";
    private const string TideAmbience = "res://assets/audio/music/generated/isle_tide_ambient.ogg";
    private const string NightAmbience = "res://assets/audio/music/generated/prison_night_ambient.ogg";

    private static readonly Vector2I Floor = new(2, 2);
    private static readonly Vector2I Accent = new(3, 4);
    private static readonly Vector2I Accent2 = new(4, 4);
    private static readonly Vector2I Wall = new(10, 4);
    private static readonly Vector2I WallAlt = new(11, 4);
    private static readonly Vector2I Bars = new(5, 2);

    private Node _liveRoot;
    private Node2D _root;
    private TileMapLayer _tiles;

    public override void _Initialize()
    {
        GD.Print("[generate-eggsile-first-night] Building the three First Night maps ...");
        _liveRoot = new Node { Name = "EggsIsleGenBuilder" };
        Root.AddChild(_liveRoot);

        BuildDock();
        BuildGatehouse();
        BuildBlock();

        GD.Print("[generate-eggsile-first-night] DONE — 3 maps saved");
        System.Environment.Exit(0);
    }

    // -----------------------------------------------------------------------
    // Map 1 — The Dock
    // -----------------------------------------------------------------------

    private void BuildDock()
    {
        _root = MakeLevel("EggsIsle", "Eggs Isle — The Dock", TideAmbience);
        _tiles = AddTileLayer(_root);

        PaintRect(-80, -26, 80, 26, Floor);
        PaintRect(-80, -26, -75, 26, Accent2);                 // sea strip (west)
        PaintRect(-74, -8, -24, -3, Accent);                    // pier walkway
        PaintRect(-74, -8, -66, -6, Accent2);                   // the boat berth
        for (int y = -26; y <= 26; y++)
            for (int x = -80; x <= 80; x++)
                if ((x + y) % 5 == 0)
                    _tiles.SetCell(new Vector2I(x, y), 0, Accent, 0);

        // --- Anchors (stable API) ---
        AddTransition(_root, "HubArrival", new Vector2(-640, 320), TransitionSide.Left, 4,
            "res://levels/eggsile/maps/EggsIsle.tscn", "HubArrival", "hub_arrival_inactive");
        AddSave(_root, "DockSavePoint", "Dock Lantern", new Vector2(-300, 300));
        AddWarp(_root, "eggsile_area1", new Vector2(-150, 300));

        // --- Arrival cutscene (AnimationPlayer scene, one-shot) ---
        AddCutsceneTrigger(_root, "ArrivalCutscene", new Vector2(-640, 296), TriggerMode.OnEnter,
            once: true, cutsceneId: "eggsile_arrival", cutsceneScene: DockArrivalCutscene,
            setFlags: new[] { "met_bacon" }, dialog: null, shapeSize: new Vector2(160, 80));

        // --- NPC: Officer Bacon on the pier ---
        InstantiateScene(_root, BaconScene, "OfficerBacon", new Vector2(-1000, -80));

        // --- Outgoing: dock → gatehouse ---
        AddTransition(_root, "DockGate", new Vector2(1280, 0), TransitionSide.Right, 8,
            "res://levels/eggsile/maps/EggsIsleGatehouse.tscn", "DockArrival", "");

        Save(_root, "res://levels/eggsile/maps/EggsIsle.tscn");
    }

    // -----------------------------------------------------------------------
    // Map 2 — The Gatehouse
    // -----------------------------------------------------------------------

    private void BuildGatehouse()
    {
        _root = MakeLevel("EggsIsleGatehouse", "Eggs Isle — Gatehouse", NightAmbience);
        _tiles = AddTileLayer(_root);

        PaintRect(-60, -30, 60, 30, Floor);
        PaintWallBand(-60, -30, 60, -27, Wall, Array.Empty<int>(), Array.Empty<int>());
        PaintWallBand(-60, 27, 60, 30, Wall, Array.Empty<int>(), Array.Empty<int>());
        PaintVWallBand(-60, -30, -57, 30, Wall, Array.Empty<int>(), Array.Empty<int>());
        PaintVWallBand(57, -30, 60, 30, Wall, Array.Empty<int>(), Array.Empty<int>());
        for (int y = -30; y <= 30; y++)
            for (int x = -60; x <= 60; x++)
                if ((x + y) % 5 == 0)
                    _tiles.SetCell(new Vector2I(x, y), 0, Accent, 0);
        PaintRect(-21, -6, -16, -3, Accent2);                   // the booking desk

        // --- Transitions (both directions) ---
        AddTransition(_root, "DockArrival", new Vector2(-960, 0), TransitionSide.Left, 8,
            "res://levels/eggsile/maps/EggsIsle.tscn", "DockGate", "");
        AddTransition(_root, "GatehouseExit", new Vector2(960, 0), TransitionSide.Right, 8,
            "res://levels/eggsile/maps/EggsIsleBlock.tscn", "GatehouseArrival", "met_tea");

        AddSave(_root, "GatehouseSavePoint", "Gatehouse Bench", new Vector2(-500, 200));

        // --- Check-in cutscene (one-shot OnEnter just inside the west door) ---
        AddCutsceneTrigger(_root, "CheckInCutscene", new Vector2(-600, 0), TriggerMode.OnEnter,
            once: true, cutsceneId: "gatehouse_checkin", cutsceneScene: CheckInCutscene,
            setFlags: null, dialog: null, shapeSize: new Vector2(120, 90));

        // --- Mr Tea + desk readables ---
        InstantiateScene(_root, MrTeaScene, "MrTea", new Vector2(-300, -60));
        AddReadable(_root, "Ledger", new Vector2(-420, -100), new[]
        {
            "Gatehouse ledger — Inmate #77: 'Eggbert.' Occupation: 'egg.' One previous conviction: existing."
        }, "read_ledger", true);
        AddReadable(_root, "RulesBoard", new Vector2(-380, 60), new[]
        {
            "OVERFLOW WING RULES — 1) Count at nine. Be present. 2) The boilers are not for warming porridge. 3) If the pipes sing, do not follow them. They lead to the tunnels. The tunnels are not a place."
        }, "read_rules", true);
        AddReadable(_root, "IntakeStamp", new Vector2(-220, -80), new[]
        {
            "The intake stamp. It says 'ACCEPTED'. It also says 'ACCEPTED' upside down. It's a versatile stamp."
        }, "read_stamp", true);

        Save(_root, "res://levels/eggsile/maps/EggsIsleGatehouse.tscn");
    }

    // -----------------------------------------------------------------------
    // Map 3 — The Overflow wing
    // -----------------------------------------------------------------------

    private void BuildBlock()
    {
        _root = MakeLevel("EggsIsleBlock", "Eggs Isle — The Overflow", NightAmbience);
        _tiles = AddTileLayer(_root);

        PaintRect(-104, -38, 104, 38, Floor);
        PaintWallBand(-104, -38, 104, -35, Wall, Array.Empty<int>(), Array.Empty<int>());
        PaintWallBand(-104, 35, 104, 38, Wall, Array.Empty<int>(), Array.Empty<int>());
        PaintVWallBand(-104, -38, -101, 38, Wall, Array.Empty<int>(), Array.Empty<int>());
        PaintVWallBand(101, -38, 104, 38, Wall, Array.Empty<int>(), Array.Empty<int>());
        for (int y = -38; y <= 38; y++)
            for (int x = -104; x <= 104; x++)
                if ((x + y) % 5 == 0)
                    _tiles.SetCell(new Vector2I(x, y), 0, Accent, 0);

        // --- Cell niche (north-west) ---
        PaintRect(-102, -36, -42, -14, Accent2);                // cell floor patch
        PaintWallBand(-104, -12, -40, -9, Bars,
            new[] { -71 }, new[] { -67 });                      // cell front: bars + open door gap
        PaintVWallBand(-40, -38, -40, -12, Wall, Array.Empty<int>(), Array.Empty<int>());  // cell east wall
        PaintWallBand(-40, -12, -21, -9, Wall, Array.Empty<int>(), Array.Empty<int>());    // wall cell→boiler
        // --- Boiler room (north-center) ---
        PaintRect(-18, -36, 32, -14, Accent2);                  // boiler room floor
        PaintRect(-16, -34, -8, -28, WallAlt);                  // the boiler itself (visual)
        AddWallRect(-16, -34, -9, -28, WallAlt);                // boiler collision block
        PaintWallBand(-20, -12, 34, -9, Wall, new[] { -5 }, new[] { -3 });   // boiler wall + door gap
        PaintVWallBand(-20, -38, -20, -12, Wall, Array.Empty<int>(), Array.Empty<int>());
        PaintVWallBand(34, -38, 34, -12, Wall, Array.Empty<int>(), Array.Empty<int>());
        PaintWallBand(35, -12, 39, -9, Wall, Array.Empty<int>(), Array.Empty<int>());    // wall boiler→tank
        // --- Tank (east, flooded) ---
        PaintRect(42, -36, 102, -14, Accent2);                  // tank floor (damp)
        PaintWallBand(40, -12, 104, -9, Wall, new[] { 60 }, new[] { 66 });  // tank front + entrance gap
        PaintVWallBand(40, -38, 40, -12, Wall, Array.Empty<int>(), Array.Empty<int>());
        // --- Gallery (south-east, collapsed) ---
        PaintRect(42, 12, 102, 36, Accent);                     // gallery floor
        PaintWallBand(40, 9, 104, 11, Wall, new[] { 60 }, new[] { 66 });   // gallery front + entrance gap
        PaintVWallBand(40, 12, 40, 36, Wall, Array.Empty<int>(), Array.Empty<int>());
        PaintWallBand(96, 12, 104, 36, WallAlt, Array.Empty<int>(), Array.Empty<int>());  // collapse rubble
        // --- Hatch alcove (south-west) ---
        PaintRect(-102, 12, -42, 36, Accent2);
        PaintWallBand(-104, 9, -40, 11, Wall, new[] { -89 }, new[] { -87 });  // hatch door gap
        PaintVWallBand(-40, 12, -40, 36, Wall, Array.Empty<int>(), Array.Empty<int>());
        PaintWallBand(-40, 9, 39, 11, Wall, Array.Empty<int>(), Array.Empty<int>());      // wall hatch→gallery

        // --- Arrival + placement cutscene (one-shot at the west gate) ---
        AddTransition(_root, "GatehouseArrival", new Vector2(-1664, 0), TransitionSide.Left, 8,
            "res://levels/eggsile/maps/EggsIsleGatehouse.tscn", "GatehouseExit", "");
        AddCutsceneTrigger(_root, "CellPlacement", new Vector2(-1500, 0), TriggerMode.OnEnter,
            once: true, cutsceneId: "cell_placement", cutsceneScene: PlacementCutscene,
            setFlags: null, dialog: null, shapeSize: new Vector2(150, 90));

        // --- The cell: Frank, bunk, reads, tunnel key ---
        InstantiateScene(_root, FrankScene, "Frank", new Vector2(-1300, -400));
        AddSave(_root, "BunkSavePoint", "Cell Niche — Bunk", new Vector2(-1480, -520));
        AddReadable(_root, "WallScratch", new Vector2(-1350, -550), new[]
        {
            "Scratched into the wall: 'SUNNY SIDE UP' and a small sun with rays. Someone drew it with something they shouldn't have had."
        }, "read_scratch", true);
        AddReadable(_root, "TinCup", new Vector2(-900, -300), new[]
        {
            "A tin cup. It rattles when the boilers hum. Frank says that's how you know the count is coming."
        }, "read_cup", true);
        AddPickup(_root, "TunnelKeyPickup", "tunnel_key", new Vector2(-1480, -500),
            new[] { "Found a heavy tunnel key tucked under the bunk. Frank's key." },
            new[] { "found_tunnel_key" }, CellKeySprite);

        // --- Boiler puzzle: crate onto plate opens the boiler door ---
        InstantiateScene(_root, MikanScene, "Mikan", new Vector2(0, -420));
        AddWallRect(11, -23, 11, -23, Wall);                    // plate backstop
        InstantiateScene(_root, CrateScene, "BoilerCrate", new Vector2(176, -496));
        AddPlate(_root, "BoilerPlate", new Vector2(176, -384), new NodePath("BoilerDoor"), "boiler_gate_open");
        AddDoor(_root, "BoilerDoor", new Vector2(-64, -168), startOpen: false);
        AddPickup(_root, "BoilerRewardPickup", "deviled_egg", new Vector2(300, -500),
            new[] { "Found a deviled egg tucked behind the boiler. The pipes kept it warm." },
            null, DeviledSprite);

        // --- Tank: Scrambles + tide pool reward ---
        InstantiateScene(_root, ScramblesScene, "Scrambles", new Vector2(1000, -400));
        AddPickup(_root, "TidePoolRewardPickup", "hardboiled_egg", new Vector2(1400, -500),
            new[] { "Found a hardboiled egg in the tide pool. Still warm. The pipes giveth." },
            null, HardboiledSprite);

        // --- Gallery + Wiener Cop in the corridor ---
        AddReadable(_root, "GallerySign", new Vector2(900, 300), new[]
        {
            "THE GALLERY — CLOSED. Collapsed years ago during the night shift. They never found the warden's filing cabinet. The count was off by one for a week."
        }, "read_gallery", true);
        InstantiateScene(_root, WienerCopScene, "WienerCop", new Vector2(700, 0));

        // --- The count (one-shot mid-corridor) ---
        AddCutsceneTrigger(_root, "CountTrigger", new Vector2(400, 0), TriggerMode.OnEnter,
            once: true, cutsceneId: "count", cutsceneScene: CountCutscene,
            setFlags: new[] { "eggsile_count_survived" }, dialog: null, shapeSize: new Vector2(100, 70));

        // --- Tunnel hatch payoff (key-gated, south-west) ---
        AddKeyDoor(_root, "TunnelHatch", new Vector2(-1408, 168), "found_tunnel_key",
            "The hatch is sealed. Mikan said the tunnels run under everything. The key on the bunk fits.");
        AddPickup(_root, "TunnelRewardPickup", "lucky_yolk", new Vector2(-1500, 350),
            new[] { "Found a lucky yolk pendant, tucked where the pipes meet the wall. It's warm." },
            new[] { "tunnel_opened" }, YolkSprite);
        AddReadable(_root, "HatchNote", new Vector2(-1300, 450), new[]
        {
            "Note in a dry corner: 'The tunnels run under everything. Follow the pipes, mind the singing, and don't tell the Count. — a friend.' Pinned beneath it, a small sun symbol."
        }, "read_hatch", true);

        Save(_root, "res://levels/eggsile/maps/EggsIsleBlock.tscn");
    }

    // -----------------------------------------------------------------------
    // Tile helpers
    // -----------------------------------------------------------------------

    private TileMapLayer AddTileLayer(Node2D root)
    {
        var layer = new TileMapLayer { Name = "CoreTilemapLayer" };
        root.AddChild(layer);
        // SetScript FIRST on the pre-AddChild wrapper, then re-fetch: SetScript
        // replaces the managed instance, disposing wrappers fetched before it.
        layer.SetScript(GD.Load<Script>(LayerScript));
        var live = root.GetNode<TileMapLayer>("CoreTilemapLayer");
        live.TileSet = GD.Load<TileSet>(Tileset);
        live.Owner = root;
        return live;
    }

    private void PaintRect(int x0, int y0, int x1, int y1, Vector2I atlas)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                _tiles.SetCell(new Vector2I(x, y), 0, atlas, 0);
    }

    /// <summary>Paints a horizontal wall band (y0..y1) with x-gaps, adding collision per solid run.</summary>
    private void PaintWallBand(int x0, int y0, int x1, int y1, Vector2I atlas, int[] gapStarts, int[] gapEnds)
    {
        var runs = new List<(int, int)>();
        int cursor = x0;
        for (int i = 0; i < gapStarts.Length; i++)
        {
            if (gapStarts[i] > cursor)
                runs.Add((cursor, gapStarts[i] - 1));
            cursor = Math.Max(cursor, gapEnds[i] + 1);
        }
        if (cursor <= x1)
            runs.Add((cursor, x1));
        foreach (var (rx0, rx1) in runs)
        {
            PaintRect(rx0, y0, rx1, y1, atlas);
            AddWallRect(rx0, y0, rx1, y1);
        }
    }

    /// <summary>Paints a vertical wall band (x0..x1) with y-gaps, adding collision per solid run.</summary>
    private void PaintVWallBand(int x0, int y0, int x1, int y1, Vector2I atlas, int[] gapStarts, int[] gapEnds)
    {
        var runs = new List<(int, int)>();
        int cursor = y0;
        for (int i = 0; i < gapStarts.Length; i++)
        {
            if (gapStarts[i] > cursor)
                runs.Add((cursor, gapStarts[i] - 1));
            cursor = Math.Max(cursor, gapEnds[i] + 1);
        }
        if (cursor <= y1)
            runs.Add((cursor, y1));
        foreach (var (ry0, ry1) in runs)
        {
            PaintRect(x0, ry0, x1, ry1, atlas);
            AddWallRect(x0, ry0, x1, ry1);
        }
    }

    private void AddWallRect(int x0, int y0, int x1, int y1)
    {
        AddWallRect(x0, y0, x1, y1, Wall);
    }

    private void AddWallRect(int x0, int y0, int x1, int y1, Vector2I atlas)
    {
        var body = new StaticBody2D
        {
            Name = $"Wall_{x0}_{y0}",
            CollisionLayer = CollisionConfig.WallsLayer,
            CollisionMask = 0
        };
        _root.AddChild(body);
        body.Owner = _root;

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D
            {
                Size = new Vector2((x1 - x0 + 1) * 16, (y1 - y0 + 1) * 16)
            }
        };
        body.AddChild(shape);
        body.Position = new Vector2((x0 + x1 + 1) * 8, (y0 + y1 + 1) * 8);
    }

    // -----------------------------------------------------------------------
    // Node builders
    // -----------------------------------------------------------------------

    private Node2D MakeLevel(string name, string levelName, string ambience)
    {
        var root = new Node2D { Name = name };
        _liveRoot.AddChild(root);
        root.SetScript(GD.Load<Script>(BaseLevelScript));
        var live = _liveRoot.GetNode<Node2D>(name);
        live.Set("LevelName", levelName);
        live.Set("LevelMusic", GD.Load<AudioStream>(Music));
        live.Set("LevelAmbience", GD.Load<AudioStream>(ambience));
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
        bool once, string cutsceneId, string cutsceneScene, string[] setFlags, string[] dialog, Vector2 shapeSize)
    {
        var trigger = GD.Load<PackedScene>(CutsceneTriggerScene).Instantiate<Area2D>();
        root.AddChild(trigger);
        trigger.Owner = root;
        trigger.Name = name;
        trigger.Position = pos;
        trigger.Set("Mode", (int)mode);
        trigger.Set("Once", once);
        trigger.Set("CutsceneId", cutsceneId);
        if (!string.IsNullOrEmpty(cutsceneScene))
            trigger.Set("CutsceneScene", GD.Load<PackedScene>(cutsceneScene));
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

    private static void AddReadable(Node2D root, string name, Vector2 pos, string[] lines, string gateFlag, bool once)
    {
        var readable = GD.Load<PackedScene>(ReadableScene).Instantiate<Area2D>();
        root.AddChild(readable);
        readable.Owner = root;
        readable.Name = name;
        readable.Position = pos;
        readable.Set("DialogLines", lines);
        readable.Set("GateFlag", gateFlag);
        readable.Set("Once", once);
    }

    private static void AddPickup(Node2D root, string name, string itemId, Vector2 pos, string[] dialog, string[] setFlags, string spritePath)
    {
        var area = new Area2D { Name = name, Position = pos };
        root.AddChild(area);
        area.SetScript(GD.Load<Script>(PickupScript));
        var liveArea = root.GetNode<Area2D>(name);
        liveArea.Owner = root;

        var sprite = new Sprite2D();
        sprite.Texture = GD.Load<Texture2D>(spritePath);
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

    private static void AddDoor(Node2D root, string name, Vector2 pos, bool startOpen)
    {
        var door = GD.Load<PackedScene>(DoorScene).Instantiate<StaticBody2D>();
        root.AddChild(door);
        door.Owner = root;
        door.Name = name;
        door.Position = pos;
        door.Set("StartOpen", startOpen);
    }

    private static void AddKeyDoor(Node2D root, string name, Vector2 pos, string requiredFlag, string lockedMessage)
    {
        var door = GD.Load<PackedScene>(KeyDoorScene).Instantiate<StaticBody2D>();
        root.AddChild(door);
        door.Owner = root;
        door.Name = name;
        door.Position = pos;
        door.Set("RequiredFlag", requiredFlag);
        door.Set("LockedMessage", lockedMessage);
    }

    private static void AddPlate(Node2D root, string name, Vector2 pos, NodePath targetDoorPath, string pressedFlag)
    {
        var plate = GD.Load<PackedScene>(PlateScene).Instantiate<Area2D>();
        root.AddChild(plate);
        plate.Owner = root;
        plate.Name = name;
        plate.Position = pos;
        plate.Set("TargetDoorPath", targetDoorPath);
        plate.Set("PushablePressedFlag", pressedFlag);
    }

    private static Node2D InstantiateScene(Node2D root, string scenePath, string name, Vector2 pos)
    {
        var node = GD.Load<PackedScene>(scenePath).Instantiate<Node2D>();
        root.AddChild(node);
        node.Owner = root;
        node.Name = name;
        node.Position = pos;
        return root.GetNode<Node2D>(name);
    }

    private static void Save(Node2D root, string path)
    {
        var packed = new PackedScene();
        packed.Pack(root);
        var err = ResourceSaver.Save(packed, path);
        GD.Print($"[generate-eggsile-first-night] Saved '{path}' -> {err}");
        root.Free();
    }
}
