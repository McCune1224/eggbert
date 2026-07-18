using System;
using System.IO;
using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// Structured file logger for Eggbert.
/// Writes to user://logs/ with timestamped files, log rotation, and leveled output.
/// Also mirrors to GD.Print/GD.PrintErr for editor console and MCP get_debug_output capture.
/// 
/// Control via env var EGGBERT_LOG_LEVEL: debug, info (default), warn, error, off.
/// Set EGGBERT_LOG_ECHO=0 to suppress GD.Print mirroring (file-only mode).
/// </summary>
public static class GameLogger
{
    public enum Level { Debug, Info, Warn, Error, Off }

    private static Level _minLevel = Level.Info;
    private static readonly object _lock = new();
    private static string _logDir;
    private static string _logPath;
    private static bool _echoToConsole = true;

    private const int MaxLogFiles = 5;
    private const string LogPrefix = "eggbert_";

    /// <summary>Call once at startup — from boot/GameInit.cs or an autoload _Ready.</summary>
    public static void Initialize(Level minLevel = Level.Info, bool echoToConsole = true)
    {
        _minLevel = minLevel;
        _echoToConsole = echoToConsole;

        _logDir = ProjectSettings.GlobalizePath("user://logs");
        DirAccess.MakeDirRecursiveAbsolute(_logDir);
        RotateOldLogs();

        string date = DateTime.Now.ToString("yyyy-MM-dd");
        _logPath = Path.Combine(_logDir, $"{LogPrefix}{date}.log");
    }

    /// <summary>Read EGGBERT_LOG_LEVEL and EGGBERT_LOG_ECHO env vars, then initialize.</summary>
    public static void InitializeFromEnv()
    {
        string envLevel = System.Environment.GetEnvironmentVariable("EGGBERT_LOG_LEVEL")?.ToLower();
        Level level = envLevel switch
        {
            "debug" => Level.Debug,
            "warn" => Level.Warn,
            "error" => Level.Error,
            "off" => Level.Off,
            _ => Level.Info,
        };

        string envEcho = System.Environment.GetEnvironmentVariable("EGGBERT_LOG_ECHO");
        bool echo = envEcho != "0";

        Initialize(level, echo);
    }

    public static void Debug(
        string tag,
        string message,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0)
    {
        if (_minLevel > Level.Debug) return;
        string src = FormatCaller(callerFile, callerLine);
        Write("DEBUG", tag, $"{message} ({src})");
        if (_echoToConsole) GD.Print($"[{tag}] {message}");
    }

    public static void Info(
        string tag,
        string message,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0)
    {
        if (_minLevel > Level.Info) return;
        string src = FormatCaller(callerFile, callerLine);
        Write("INFO", tag, $"{message} ({src})");
        if (_echoToConsole) GD.Print($"[{tag}] {message}");
    }

    public static void Warn(
        string tag,
        string message,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0)
    {
        if (_minLevel > Level.Warn) return;
        string src = FormatCaller(callerFile, callerLine);
        Write("WARN", tag, $"{message} ({src})");
        if (_echoToConsole) GD.Print($"[{tag}] {message}");
    }

    public static void Error(
        string tag,
        string message,
        [CallerFilePath] string callerFile = "",
        [CallerLineNumber] int callerLine = 0)
    {
        if (_minLevel > Level.Error) return;
        string src = FormatCaller(callerFile, callerLine);
        Write("ERROR", tag, $"{message} ({src})");
        if (_echoToConsole) GD.PrintErr($"[{tag}] {message}");
    }

    private static string FormatCaller(string file, int line)
    {
        if (string.IsNullOrEmpty(file)) return "";
        string shortPath = file.Replace("\\", "/");
        int idx = shortPath.LastIndexOf("/");
        return idx >= 0 ? $"{shortPath.Substring(idx + 1)}:{line}" : $"{file}:{line}";
    }

    private static void Write(string level, string tag, string message)
    {
        if (_logPath == null) return;

        string ts = DateTime.Now.ToString("HH:mm:ss.fff");
        string line = $"[{ts}] {level,-5} [{tag}] {message}";

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logPath, line + System.Environment.NewLine);
            }
            catch (System.Exception ex)
            {
                // Fall back to GD.PrintErr so logging failures are never invisible.
                // File I/O failure (permissions, disk full) shouldn't crash the game.
                GD.PrintErr($"[GameLogger] Write failed: {ex.GetType().Name} — {ex.Message}");
            }
        }
    }

    /// <summary>Write to file only — no GD.Print echo. Used by GameLogBridge.</summary>
    internal static void LogToFile(string level, string tag, string message) => Write(level, tag, message);

    private static void RotateOldLogs()
    {
        if (!DirAccess.DirExistsAbsolute(_logDir)) return;

        var files = new System.Collections.Generic.List<string>();
        using var dir = DirAccess.Open(_logDir);
        if (dir == null) return;

        dir.ListDirBegin();
        string fileName;
        while ((fileName = dir.GetNext()) != "")
        {
            if (fileName.StartsWith(LogPrefix) && fileName.EndsWith(".log"))
                files.Add(Path.Combine(_logDir, fileName));
        }
        dir.ListDirEnd();

        files.Sort();
        while (files.Count >= MaxLogFiles)
        {
            try { DirAccess.RemoveAbsolute(files[0]); }
            catch { /* best-effort cleanup */ }
            files.RemoveAt(0);
        }
    }

    /// <summary>Path to current log file, or null if not initialized.</summary>
    public static string CurrentLogPath => _logPath;
}
