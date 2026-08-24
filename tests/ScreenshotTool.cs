using Godot;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

// Renders real in-game screenshots of level/scene content via the Godot renderer
// (NOT --headless, which disables rendering). Run WITHOUT --headless so the
// renderer is active; the script adds each scene to the root, frames a Camera2D
// to the union of tilemap used-rects, waits for frames to draw, then saves a PNG.
//
//   godot --path . --script res://tests/ScreenshotTool.cs
//
// Output goes to docs/AUTHORING_GUIDE/assets/shots/<SceneName>.png.
// This is a content/authoring aid, not a verifier — it always exits 0.

public partial class ScreenshotTool : SceneTree
{
    private static readonly string[] Scenes = new[]
    {
        "res://levels/factory/maps/OpeningZone.tscn",
        "res://levels/factory/maps/SortingFloor.tscn",
        "res://levels/factory/maps/AssemblyLine.tscn",
        "res://levels/factory/maps/ControlRoom.tscn",
        "res://levels/factory/maps/LoadingBay.tscn",
    };

    private const string OutDir = "res://docs/AUTHORING_GUIDE/assets/shots";
    private const int CapW = 1280;
    private const int CapH = 720;

    private readonly List<string> _errors = new();

    public override async void _Initialize()
    {
        // Bigger capture resolution for readable shots.
        Root.Size = new Vector2I(CapW, CapH);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        string outDir = ProjectSettings.GlobalizePath(OutDir);
        Directory.CreateDirectory(outDir);

        foreach (var path in Scenes)
        {
            await Capture(path, outDir);
            await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        }

        foreach (var e in _errors)
            GD.PrintErr($"[screenshot] {e}");

        GD.Print($"[screenshot] captured {Scenes.Length} scene(s) -> {outDir}");
        Quit(0);
    }

    private async Task Capture(string path, string outDir)
    {
        var scene = ResourceLoader.Load<PackedScene>(path);
        if (scene == null)
        {
            _errors.Add($"could not load {path}");
            return;
        }

        Node inst = scene.Instantiate();
        Root.AddChild(inst);

        // Let the level _Ready run (audio, tilemap bounds, borders).
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        var (center, size) = ComputeBounds(inst);

        var cam = new Camera2D();
        inst.AddChild(cam);
        cam.MakeCurrent();
        cam.GlobalPosition = center;
        float zoom = Mathf.Min((float)CapW / Mathf.Max(size.X, 1f), (float)CapH / Mathf.Max(size.Y, 1f)) * 0.92f;
        cam.Zoom = new Vector2(zoom, zoom);

        // Draw frames with the new camera.
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);

        var tex = Root.GetTexture();
        if (tex == null)
        {
            _errors.Add($"{path}: viewport texture is null");
            inst.QueueFree();
            return;
        }

        var img = tex.GetImage();
        string name = Path.GetFileNameWithoutExtension(path) + ".png";
        string outp = Path.Combine(outDir, name);
        img.SavePng(outp);
        GD.Print($"[screenshot] saved {outp} ({img.GetWidth()}x{img.GetHeight()})");

        inst.QueueFree();
        await ToSignal(this, SceneTree.SignalName.ProcessFrame);
    }

    private (Vector2 center, Vector2 size) ComputeBounds(Node root)
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        bool found = false;

        CollectTilemapBounds(root, ref min, ref max, ref found);

        if (!found)
            return (Vector2.Zero, new Vector2(640f, 360f));

        Vector2 size = max - min;
        Vector2 center = (min + max) / 2f;
        return (center, size);
    }

    private void CollectTilemapBounds(Node node, ref Vector2 min, ref Vector2 max, ref bool found)
    {
        if (node is TileMapLayer tm && tm.TileSet != null)
        {
            var used = tm.GetUsedRect();
            if (used.Size.X > 0 && used.Size.Y > 0)
            {
                Vector2 tl = tm.ToGlobal(tm.MapToLocal(used.Position));
                Vector2 br = tm.ToGlobal(tm.MapToLocal(used.End));
                min = new Vector2(Mathf.Min(min.X, tl.X), Mathf.Min(min.Y, tl.Y));
                max = new Vector2(Mathf.Max(max.X, br.X), Mathf.Max(max.Y, br.Y));
                found = true;
            }
        }

        foreach (Node child in node.GetChildren())
            CollectTilemapBounds(child, ref min, ref max, ref found);
    }
}
