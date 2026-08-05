using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// One-shot headless helper that generates three interconnected "sandbox" test levels
/// under res://levels/sandbox/maps/ using Godot's own scene serialization (so the
/// tile_map_data bytes are always correct).
///
///   SandboxHub       — stone plaza hub with a save point, a warp crystal, and 2 exits.
///   SandboxGrasslands— wide open dirt field for combat/movement testing + an Oatmeal battle.
///   SandboxDepths    — stone floors with StaticBody2D walls forming a short maze.
///
/// Run with: godot --headless --path . --script res://tests/GenerateSandboxLevels.cs
/// </summary>
public partial class GenerateSandboxLevels : SceneTree
{
    private const string MixelTileset = "res://assets/tilemaps/mixel_ground_tileset.tres";
    private const string BaseLevelScript = "res://levels/BaseLevel.cs";
    private const string LayerScript = "res://components/core/LevelTileMapLayer.cs";
    private const string WarpScene = "res://components/maps/WarpPoint.tscn";
    private const string SaveScene = "res://saves/SavePoint.tscn";
    private const string TransitionScene = "res://levels/LevelTransition.tscn";
    private const string PickupScript = "res://components/items/PickupItem.cs";
    private const string EncounterScript = "res://components/npcs/MercyEncounterTrigger.cs";
    private const string EggSprite = "res://assets/items/sprites/item_sprite_0020.png";

    private Node _liveRoot;

    public override void _Initialize()
    {
        GD.Print("[generate-sandbox] Building sandbox levels...");
        _liveRoot = new Node { Name = "SandboxGenBuilder" };
        Root.AddChild(_liveRoot);

        BuildHub();
        BuildGrasslands();
        BuildDepths();

        GD.Print("[generate-sandbox] DONE — 3 sandbox levels saved under res://levels/sandbox/maps/");
        System.Environment.Exit(0);
    }

    private void BuildHub()
    {
        var root = MakeLevel("SandboxHub", "Sandbox Hub");
        var tiles = AddTiles(root, 28, 16, atlasStone); // stone plaza (x -448..448, y -256..256)

        // Small dirt "rest" patch near center for visual interest.
        for (int x = 11; x <= 16; x++)
            for (int y = 6; y <= 9; y++)
                tiles.SetCell(new Vector2I(x - 14, y - 8), 0, atlasDirt, 0);

        // Player spawn / warp + save point.
        AddWarp(root, "sandbox_hub", new Vector2(0, 0));
        AddSave(root, "Sandbox Hub", new Vector2(64, 64));

        // North exit <-> Grasslands (target is Grasslands' own door so it works both ways).
        AddTransition(root, "NorthToGrasslands", new Vector2(0, -232), TransitionSide.Up, 5,
            "res://levels/sandbox/maps/SandboxGrasslands.tscn", "SouthToHub", "");
        // West exit <-> Depths.
        AddTransition(root, "WestToDepths", new Vector2(-432, 0), TransitionSide.Left, 5,
            "res://levels/sandbox/maps/SandboxDepths.tscn", "EastToHub", "");

        // East exit <-> Eggsile Area 1 (factory exit destination).
        AddTransition(root, "EastToArea1", new Vector2(440, 0), TransitionSide.Right, 5,
            "res://levels/eggsile/maps/EggsIsle.tscn", "SandboxArrival", "");

        // Clear east wall 5-cell opening (x=13, y=-2..2) and place passage floor.
        for (int y = -2; y <= 2; y++)
            tiles.SetCell(new Vector2I(13, y), -1);
        for (int y = -2; y <= 2; y++)
            tiles.SetCell(new Vector2I(13, y), 0, atlasStone, 0);

        Save(root, "res://levels/sandbox/maps/SandboxHub.tscn");
    }

    private void BuildGrasslands()
    {
        var root = MakeLevel("SandboxGrasslands", "Sandbox Grasslands");
        var tiles = AddTiles(root, 30, 16, atlasDirt); // dirt field (x -480..480, y -256..256)

        // A stone path running east-west through the middle.
        for (int x = 0; x < 30; x++)
        {
            tiles.SetCell(new Vector2I(x - 15, 4), 0, atlasStone, 0);
            tiles.SetCell(new Vector2I(x - 15, 5), 0, atlasStone, 0);
        }

        AddWarp(root, "sandbox_grasslands", new Vector2(0, 96));
        AddSave(root, "Sandbox Grasslands", new Vector2(-80, -192));

        // Oatmeal fight for combat testing.
        AddEncounter(root, new Vector2(160, -120));

        // A couple of pickups.
        AddPickup(root, "hardboiled_egg", new Vector2(-320, 160), new[] { "Found a Hardboiled Egg. Perfect for a quick test-run snack." });
        AddPickup(root, "scrambled_egg", new Vector2(320, 160), new[] { "Found a Scrambled Egg! Testing pickups is egg-citing." });

        // South exit <-> Hub (target is the Hub's north door for a two-way link).
        AddTransition(root, "SouthToHub", new Vector2(0, 230), TransitionSide.Down, 5,
            "res://levels/sandbox/maps/SandboxHub.tscn", "NorthToGrasslands", "");

        Save(root, "res://levels/sandbox/maps/SandboxGrasslands.tscn");
    }

    private void BuildDepths()
    {
        var root = MakeLevel("SandboxDepths", "Sandbox Depths");
        var tiles = AddTiles(root, 22, 16, atlasStone); // x -352..352, y -256..256

        // Two interior StaticBody2D walls to form a short corridor maze.
        AddWall(root, "WallA", new Vector2(0, -130), new Vector2(560, 24));
        AddWall(root, "WallB", new Vector2(-90, -60), new Vector2(24, 320));

        AddWarp(root, "sandbox_depths", new Vector2(0, 64));
        AddPickup(root, "hardboiled_egg", new Vector2(230, 192), new[] { "Found a Hardboiled Egg. The depths are deep, but so is your bag." });

        // East exit <-> Hub (target is the Hub's west door for a two-way link).
        AddTransition(root, "EastToHub", new Vector2(336, 0), TransitionSide.Right, 5,
            "res://levels/sandbox/maps/SandboxHub.tscn", "WestToDepths", "");

        Save(root, "res://levels/sandbox/maps/SandboxDepths.tscn");
    }

    // -----------------------------------------------------------------------
    // Builders
    // -----------------------------------------------------------------------
    private static readonly Vector2I atlasStone = new Vector2I(0, 0);
    private static readonly Vector2I atlasDirt = new Vector2I(0, 4);

    private Node2D MakeLevel(string name, string levelName)
    {
        var root = new Node2D { Name = name };
        _liveRoot.AddChild(root);
        root.SetScript(GD.Load<Script>(BaseLevelScript));
        var live = _liveRoot.GetNode<Node2D>(name);
        live.Set("LevelName", levelName);
        return live;
    }

    private TileMapLayer AddTiles(Node2D root, int width, int height, Vector2I atlas)
    {
        var layer = new TileMapLayer { Name = "CoreTilemapLayer" };
        root.AddChild(layer);
        layer.SetScript(GD.Load<Script>(LayerScript));
        var live = root.GetNode<TileMapLayer>("CoreTilemapLayer");
        live.TileSet = GD.Load<TileSet>(MixelTileset);
        live.Owner = root;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                live.SetCell(new Vector2I(x - width / 2, y - height / 2), 0, atlas, 0);
        return live;
    }

    private static Area2D AddWarp(Node2D root, string warpId, Vector2 pos)
    {
        var warp = GD.Load<PackedScene>(WarpScene).Instantiate<Area2D>();
        root.AddChild(warp);
        warp.Owner = root;
        warp.Position = pos;
        warp.Set("WarpId", warpId);
        return warp;
    }

    private static void AddSave(Node2D root, string location, Vector2 pos)
    {
        var save = GD.Load<PackedScene>(SaveScene).Instantiate<Area2D>();
        root.AddChild(save);
        save.Owner = root;
        save.Position = pos;
        save.Set("LocationName", location);
    }

    private static void AddTransition(Node2D root, string name, Vector2 pos, TransitionSide side, int size,
        string level, string target, string requiredFlag)
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
    }

    private static void AddPickup(Node2D root, string itemId, Vector2 pos, string[] dialog)
    {
        var area = new Area2D { Name = $"{itemId}Pickup", Position = pos };
        root.AddChild(area);
        area.SetScript(GD.Load<Script>(PickupScript));
        var liveArea = root.GetNode<Area2D>($"{itemId}Pickup");
        liveArea.Owner = root;

        var sprite = new Sprite2D();
        sprite.Texture = GD.Load<Texture2D>(EggSprite);
        liveArea.AddChild(sprite);
        sprite.Owner = root;

        var shape = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = 16f }
        };
        liveArea.AddChild(shape);
        shape.Owner = root;

        liveArea.Set("ItemId", itemId);
        liveArea.Set("Count", 1);
        liveArea.Set("DialogLines", dialog);
    }

    private static void AddEncounter(Node2D root, Vector2 pos)
    {
        var area = new Area2D { Name = "OatmealEncounter", Position = pos };
        root.AddChild(area);
        area.SetScript(GD.Load<Script>(EncounterScript));
        var liveArea = root.GetNode<Area2D>("OatmealEncounter");
        liveArea.Owner = root;

        var shape = new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = new Vector2(80, 64) }
        };
        liveArea.AddChild(shape);
        shape.Owner = root;

        liveArea.Set("SpareFlag", "sandbox_spared_oatmeal");
        liveArea.Set("FightFlag", "sandbox_fought_oatmeal");
        liveArea.Set("BeatFlag", "sandbox_beat_oatmeal");
        liveArea.Set("OnceFlag", "sandbox_resolved_oatmeal");
        liveArea.Set("IntroLines", new[] {
            "A stray Oatmeal oozes out of the tall grass, bubbling with suspicion.",
            "It has nowhere else to be. Neither do you."
        });
        liveArea.Set("SpareLines", new[] {
            "You offer a calm wave. The Oatmeal settles, steaming contentedly.",
            "'First day out,' it burbles. 'Might wander the plains a while.'",
            "It rolls off toward a tuft of grass. Combat resolved."
        });
        liveArea.Set("FightLines", new[] {
            "You ready yourself. The Oatmeal roils and lunges!",
            "A test-run tussle begins."
        });
    }

    private static void AddWall(Node2D root, string name, Vector2 pos, Vector2 size)
    {
        var body = new StaticBody2D { Name = name, Position = pos };
        body.CollisionLayer = CollisionConfig.WallsLayer;
        body.CollisionMask = 0;
        var shape = new CollisionShape2D { Shape = new RectangleShape2D { Size = size } };
        body.AddChild(shape);
        root.AddChild(body);
        body.Owner = root;
        shape.Owner = root;
    }

    private static void Save(Node2D root, string path)
    {
        var packed = new PackedScene();
        packed.Pack(root);
        var err = ResourceSaver.Save(packed, path);
        GD.Print($"[generate-sandbox] Saved '{path}' -> {err}");
        root.Free();
    }
}