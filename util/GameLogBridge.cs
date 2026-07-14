using Godot;

/// <summary>
/// Bridges Godot's Logger system to GameLogger, capturing engine-level
/// errors and warnings into the file log. Registered via OS.AddLogger().
/// 
/// Note: Godot calls _LogError/_LogMessage from arbitrary threads —
/// GameLogger's file write handles this with its internal lock.
/// </summary>
public partial class GameLogBridge : Logger
{
    public override void _LogError(
        string function,
        string file,
        int line,
        string code,
        string rationale,
        bool editorNotify,
        int errorType,
        Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
    {
        string tag = errorType switch
        {
            (int)ErrorType.Warning => "Engine/Warn",
            (int)ErrorType.Script => "Engine/Script",
            (int)ErrorType.Shader => "Engine/Shader",
            _ => "Engine/Error"
        };

        string msg = $"{rationale} [{file}:{line} @ {function}()]";
        if (!string.IsNullOrEmpty(code))
            msg += $" | code: {code}";

        if (errorType == (int)ErrorType.Warning)
            GameLogger.LogToFile("WARN", tag, msg);
        else
            GameLogger.LogToFile("ERROR", tag, msg);
    }

    public override void _LogMessage(string message, bool error)
    {
        // Engine print() messages — most are already captured by our GD.Print mirror.
        // Only write to file to avoid doubling console output.
        string msg = message.TrimEnd('\n');
        if (error)
            GameLogger.LogToFile("ERROR", "Engine/Msg", msg);
        else
            GameLogger.LogToFile("DEBUG", "Engine/Msg", msg);
    }
}
