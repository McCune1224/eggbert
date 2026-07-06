using Godot;
using Godot.Collections;

/// <summary>Types of actions that can be sequenced in a cutscene.</summary>
public enum CutsceneActionType
{
    LockPlayer,
    UnlockPlayer,
    MoveNpc,
    SayDialog,
    Wait,
    SetFlag,
    Fade
}

/// <summary>A single step in a cutscene sequence.</summary>
public struct CutsceneAction
{
    public CutsceneActionType Type;
    public Dictionary<string, Variant> Params;

    public CutsceneAction(CutsceneActionType type, Dictionary<string, Variant> parms = null)
    {
        Type = type;
        Params = parms ?? new Dictionary<string, Variant>();
    }

    // -- Convenience constructors --

    public static CutsceneAction LockPlayer() =>
        new(CutsceneActionType.LockPlayer);

    public static CutsceneAction UnlockPlayer() =>
        new(CutsceneActionType.UnlockPlayer);

    public static CutsceneAction MoveNpc(string npcPath, Vector2 targetPosition, float duration = 1.0f) =>
        new(CutsceneActionType.MoveNpc, new Dictionary<string, Variant>
        {
            { "npc_path", npcPath },
            { "target_position", targetPosition },
            { "duration", duration }
        });

    public static CutsceneAction SayDialog(string[] lines) =>
        new(CutsceneActionType.SayDialog, new Dictionary<string, Variant>
        {
            { "lines", new Array<string>(lines) }
        });

    public static CutsceneAction Wait(float seconds) =>
        new(CutsceneActionType.Wait, new Dictionary<string, Variant>
        {
            { "duration", seconds }
        });

    public static CutsceneAction SetFlag(string key, Variant value) =>
        new(CutsceneActionType.SetFlag, new Dictionary<string, Variant>
        {
            { "key", key },
            { "value", value }
        });

    public static CutsceneAction Fade(string type) =>
        new(CutsceneActionType.Fade, new Dictionary<string, Variant>
        {
            { "type", type }
        });
}
