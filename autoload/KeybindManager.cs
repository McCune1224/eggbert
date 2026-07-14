using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages customizable keybindings. Loads saved bindings on _Ready,
/// provides rebind/reset/save/load, and exposes display helpers.
/// Registered in project.godot as an autoload singleton.
/// </summary>
public partial class KeybindManager : Node
{
    private static KeybindManager _instance;
    public static KeybindManager Instance => _instance;

    // Default binding(s) per action. Primary key listed first.
    private static readonly Dictionary<string, Key[]> DefaultBindings = new()
    {
        { "player_up", new[] { Key.W } },
        { "player_down", new[] { Key.S } },
        { "player_left", new[] { Key.A } },
        { "player_right", new[] { Key.D } },
        { "interact", new[] { Key.E } },
        { "menu_pause", new[] { Key.Escape, Key.F1 } },
        { "player_sprint", new[] { Key.Shift } },
        { "dash", new[] { Key.Space } },
        { "combat_parry", new[] { Key.J } },
    };

    // Stable order for settings UI and save iteration.
    public static readonly string[] RebindableActions =
    {
        "player_up", "player_down", "player_left", "player_right",
        "interact", "menu_pause", "player_sprint", "dash", "combat_parry",
    };

    // --- Public API ------------------------------------------------

    public static string GetActionDisplayName(string action)
    {
        return action switch
        {
            "player_up" => "Move Up",
            "player_down" => "Move Down",
            "player_left" => "Move Left",
            "player_right" => "Move Right",
            "interact" => "Interact",
            "menu_pause" => "Pause / Menu",
            "player_sprint" => "Sprint",
            "dash" => "Dash",
            "combat_parry" => "Parry",
            _ => action,
        };
    }

    /// Returns the physical keycode of the first keyboard event for an action.
    public static Key GetCurrentKey(string action)
    {
        foreach (var ev in InputMap.ActionGetEvents(action))
        {
            if (ev is InputEventKey k)
                return k.PhysicalKeycode;
        }
        return Key.None;
    }

    /// Returns a human-readable label for the action's current primary key.
    public static string GetCurrentKeyLabel(string action)
    {
        Key key = GetCurrentKey(action);
        return key == Key.None ? "—" : OS.GetKeycodeString(key);
    }

    /// Removes all keyboard events for an action and adds a single new one.
    public static void RebindAction(string action, Key newKey)
    {
        var events = InputMap.ActionGetEvents(action);
        foreach (var ev in events)
        {
            if (ev is InputEventKey)
                InputMap.ActionEraseEvent(action, ev);
        }

        var newEvent = new InputEventKey();
        newEvent.PhysicalKeycode = newKey;
        InputMap.ActionAddEvent(action, newEvent);
    }

    /// Restores the default binding(s) for a single action.
    public static void ResetAction(string action)
    {
        if (!DefaultBindings.ContainsKey(action)) return;

        // Remove all keyboard events.
        var events = InputMap.ActionGetEvents(action);
        foreach (var ev in events)
        {
            if (ev is InputEventKey)
                InputMap.ActionEraseEvent(action, ev);
        }

        // Add all default keyboard events.
        foreach (Key key in DefaultBindings[action])
        {
            var ev = new InputEventKey();
            ev.PhysicalKeycode = key;
            InputMap.ActionAddEvent(action, ev);
        }
    }

    /// Restores every rebindable action to its default(s).
    public static void ResetAllBindings()
    {
        foreach (string action in RebindableActions)
            ResetAction(action);
    }

    public static void SaveBindings()
    {
        var config = new ConfigFile();
        foreach (string action in RebindableActions)
        {
            Key key = GetCurrentKey(action);
            config.SetValue("bindings", action, (int)key);
        }
        config.Save("user://keybinds.cfg");
    }

    public static void LoadBindings()
    {
        var config = new ConfigFile();
        if (config.Load("user://keybinds.cfg") != Error.Ok)
            return;

        foreach (string action in RebindableActions)
        {
            if (!config.HasSectionKey("bindings", action)) continue;
            int raw = (int)config.GetValue("bindings", action, 0);
            if (raw > 0)
                RebindAction(action, (Key)raw);
        }
    }

    // --- Lifecycle ------------------------------------------------

    public override void _Ready()
    {
        if (_instance == null)
            _instance = this;
        else
            QueueFree();

        // Switch default menu_pause primary to Escape for sanity, but keep F1
        // as secondary. LoadBindings will override if the user has saved bindings.
        ResetAllBindings();
        LoadBindings();
    }
}
