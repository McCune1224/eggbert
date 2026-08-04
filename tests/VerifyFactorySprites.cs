using Godot;
using System;
using System.Collections.Generic;

// Headless verifier: every puzzle item and interactable in the Factory scenes
// must carry a visible sprite (non-null texture), and any region_rect used
// must lie inside the texture bounds (an out-of-bounds region renders nothing,
// i.e. an invisible puzzle item).
//
// Run with: godot --headless --path . --script res://tests/VerifyFactorySprites.cs
public partial class VerifyFactorySprites : SceneTree
{
    private int _failures;

    public override void _Initialize()
    {
        GD.Print("[factory-sprites] Starting sprite-coverage verification...");

        VerifyBaseComponentScenes();

        VerifyMapScene("res://levels/factory/maps/OpeningZone.tscn", new[]
        {
            "TimeClock",
            "TimeCardRack",
            "VendingMachine",
            "WarpPoint",
        });

        VerifyMapScene("res://levels/factory/maps/SortingFloor.tscn", new[]
        {
            "FactoryJamitor",
            "FactoryCrate",
            "FactoryPressurePlate",
            "CrateGate",
        });

        VerifyMapScene("res://levels/factory/maps/AssemblyLine.tscn", new[]
        {
            "Conveyor01", "Conveyor02", "Conveyor03", "Conveyor04",
            "Conveyor05", "Conveyor06", "Conveyor07", "Conveyor08",
            "Conveyor09", "Conveyor10", "Conveyor11", "Conveyor12",
            "MaintenancePadWest",
            "MaintenancePadEast",
            "ShutdownChecklist",
            "ConveyorInstruction",
            "MaintenanceTransferInstruction",
        });

        VerifyMapScene("res://levels/factory/maps/ControlRoom.tscn", new[]
        {
            "InspectionDoor",
            "SequencePlateA",
            "SequencePlateB",
            "SequencePlateC",
            "EggdropSoupPickup",
        });

        VerifyMapScene("res://levels/factory/maps/LoadingBay.tscn", new[]
        {
            "LoadingTimedGate",
            "LoadingTimedSwitchWest",
            "LoadingTimedSwitchEast",
            "CargoManifest",
            "LoadingBayWarning",
        });

        VerifyMapScene("res://levels/factory/maps/Zone1.tscn", new[]
        {
            "HubHazard1",
            "HubHazard2",
            "HubHazard3",
        });

        if (_failures == 0)
            GD.Print("[factory-sprites] ALL CHECKS PASSED");
        else
            GD.PrintErr($"[factory-sprites] {_failures} FAILURE(S)");
        System.Environment.Exit(_failures == 0 ? 0 : 1);
    }

    // -----------------------------------------------------------------------
    // Base component scenes (shared across all levels)
    // -----------------------------------------------------------------------
    private void VerifyBaseComponentScenes()
    {
        foreach (string scenePath in new[]
        {
            "res://components/puzzles/ConveyorTile.tscn",
            "res://components/puzzles/TimedSpikes.tscn",
            "res://components/puzzles/TeleportPad.tscn",
            "res://components/maps/WarpPoint.tscn",
        })
        {
            var packed = ResourceLoader.Load<PackedScene>(scenePath);
            if (packed == null)
            {
                Fail($"[base] Could not load {scenePath}");
                continue;
            }
            var root = packed.Instantiate() as Node2D;
            if (root == null)
            {
                Fail($"[base] Could not instantiate {scenePath}");
                continue;
            }
            bool hasSprite = HasVisibleSprite(root, out string detail);
            if (!hasSprite)
                Fail($"[base] {scenePath}: {detail}");
            else
                GD.Print($"[base] {scenePath}: {detail}");
            root.Free();
        }
    }

    // -----------------------------------------------------------------------
    // Map scenes
    // -----------------------------------------------------------------------
    private void VerifyMapScene(string scenePath, string[] nodeNames)
    {
        if (!FileAccess.FileExists(scenePath))
        {
            Fail($"[map] Scene not found: {scenePath}");
            return;
        }

        var packed = ResourceLoader.Load<PackedScene>(scenePath);
        if (packed == null)
        {
            Fail($"[map] Could not load {scenePath}");
            return;
        }

        var root = packed.Instantiate() as Node2D;
        if (root == null)
        {
            Fail($"[map] Could not instantiate {scenePath}");
            return;
        }

        foreach (string nodeName in nodeNames)
        {
            var node = root.GetNodeOrNull<Node2D>(nodeName);
            if (node == null)
            {
                Fail($"[{scenePath}] Missing direct-root node '{nodeName}'");
                continue;
            }

            if (!HasVisibleSprite(node, out string detail))
                Fail($"[{scenePath}] '{nodeName}': {detail}");
            else
                GD.Print($"[{scenePath}] '{nodeName}': {detail}");
        }

        root.Free();
    }

    // -----------------------------------------------------------------------
    // Core check: node (or a descendant) must contain a Sprite2D with a
    // non-null texture and a region inside the texture bounds.
    // -----------------------------------------------------------------------
    private bool HasVisibleSprite(Node2D node, out string detail)
    {
        var sprite = node as Sprite2D ?? FindSprite(node);
        if (sprite == null)
        {
            detail = "no Sprite2D found anywhere in subtree";
            return false;
        }

        if (sprite.Texture == null)
        {
            detail = $"Sprite2D '{sprite.Name}' has no texture";
            return false;
        }

        if (sprite.Visible == false)
        {
            detail = $"Sprite2D '{sprite.Name}' is hidden";
            return false;
        }

        var texSize = sprite.Texture.GetSize();
        if (texSize == Vector2.Zero)
        {
            detail = $"Sprite2D '{sprite.Name}' texture has zero size";
            return false;
        }

        if (sprite.RegionEnabled)
        {
            var region = sprite.RegionRect;
            if (region.Size == Vector2.Zero)
            {
                detail = $"Sprite2D '{sprite.Name}' region is empty";
                return false;
            }
            if (region.Position.X < 0 || region.Position.Y < 0 ||
                region.End.X > texSize.X || region.End.Y > texSize.Y)
            {
                detail = $"Sprite2D '{sprite.Name}' region {region} is outside texture bounds {texSize} — renders invisible";
                return false;
            }
        }

        detail = $"visible ({sprite.Texture.ResourcePath}, tex={texSize})";
        return true;
    }

    private Sprite2D FindSprite(Node parent)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child is Sprite2D s && s.Texture != null)
                return s;
            var nested = FindSprite(child);
            if (nested != null)
                return nested;
        }
        return null;
    }

    private void Fail(string msg)
    {
        _failures++;
        GD.PrintErr(msg);
    }
}
