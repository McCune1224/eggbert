using Godot;
using Godot.Collections;

/// <summary>Types of actions that can be sequenced in a cutscene.</summary>
public enum CutsceneActionType
{
    LockPlayer,
    UnlockPlayer,
    MoveNpc,
    MovePlayer,
    FaceDirection,
    PlayAnimation,
    CameraMove,
    SayDialog,
    Wait,
    SetFlag,
    Fade,
    PromptChoice,
    Stop
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

    public static CutsceneAction MovePlayer(Vector2 targetPosition, float duration = 1.0f) =>
        new(CutsceneActionType.MovePlayer, new Dictionary<string, Variant>
        {
            { "target_position", targetPosition },
            { "duration", duration }
        });

    public static CutsceneAction FaceDirection(string nodePath, string direction) =>
        new(CutsceneActionType.FaceDirection, new Dictionary<string, Variant>
        {
            { "node_path", nodePath },
            { "direction", direction }
        });

    public static CutsceneAction PlayAnimation(string nodePath, string animationName) =>
        new(CutsceneActionType.PlayAnimation, new Dictionary<string, Variant>
        {
            { "node_path", nodePath },
            { "animation_name", animationName }
        });

    public static CutsceneAction CameraMove(Vector2 targetPosition, float duration = 1.0f) =>
        new(CutsceneActionType.CameraMove, new Dictionary<string, Variant>
        {
            { "target_position", targetPosition },
            { "duration", duration }
        });

    public static CutsceneAction Stop() =>
        new(CutsceneActionType.Stop);

    public static CutsceneAction MoveNpc(string npcPath, Vector2 targetPosition, float duration = 1.0f) =>
        new(CutsceneActionType.MoveNpc, new Dictionary<string, Variant>
        {
            { "npc_path", npcPath },
            { "target_position", targetPosition },
            { "duration", duration }
        });

    public static CutsceneAction SayDialog(string[] lines, DialogVoiceResource voice = null)
    {
        var dict = new Dictionary<string, Variant>
        {
            { "lines", new Array<string>(lines) }
        };
        if (voice != null)
            dict["voice"] = voice;
        return new(CutsceneActionType.SayDialog, dict);
    }

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

    public static CutsceneAction PromptChoice(string[] choices, string[] flagKeys) =>
        new(CutsceneActionType.PromptChoice, new Dictionary<string, Variant>
        {
            { "choices", new Array<string>(choices) },
            { "flag_keys", new Array<string>(flagKeys) }
        });
}
