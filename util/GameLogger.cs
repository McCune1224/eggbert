using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Godot;

/// <summary>
/// Structured file logger for Eggbert.
/// Writes to user://logs/ with timestamped files, log rotation, and leveled output.
/// Also mirrors to GD.Print/GD.PrintErr for editor console and MCP get_debug_output capture.
///
/// Control via env var EGGBERT_LOG_LEVEL: debug, info (default), warn, error, off.
/// Set EGGBERT_LOG_ECHO=0 to suppress GD.Print mirroring (file-only mode).
///
/// Since 2026-08-03 the logger also writes a machine-readable JSONL mirror
/// (eggbert_YYYY-MM-DD.jsonl) — one JSON object per line — designed for AI
/// agents and external tooling to parse, query, and share across sessions.
/// Each entry carries a session id (EGGBERT_SESSION_ID env override, else a
/// per-boot id) so a single play session can be traced across files.
/// </summary>
public static class GameLogger
{
    public enum Level { Debug, Info, Warn, Error, Off }

    private static Level _minLevel = Level.Info;
    private static readonly object _lock = new();
    private static string _logDir;
    private static string _logPath;
    private static string _jsonlPath;
    private static string _sessionId;
    private static bool _echoToConsole = true;

    private const int MaxLogFiles = 5;
    private const string LogPrefix = "eggbert_";
    private const string SessionIdEnvVar = "EGGBERT_SESSION_ID";

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
        _jsonlPath = Path.Combine(_logDir, $"{LogPrefix}{date}.jsonl");

        _sessionId = System.Environment.GetEnvironmentVariable(SessionIdEnvVar);
        if (string.IsNullOrWhiteSpace(_sessionId))
            _sessionId = Guid.NewGuid().ToString("N")[..12];

        Info("GameLogger", $"session={_sessionId} jsonl={Path.GetFileName(_jsonlPath)}");
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
        Write("DEBUG", tag, message, src);
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
        Write("INFO", tag, message, src);
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
        Write("WARN", tag, message, src);
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
        Write("ERROR", tag, message, src);
        if (_echoToConsole) GD.PrintErr($"[{tag}] {message}");
    }

    private static string FormatCaller(string file, int line)
    {
        if (string.IsNullOrEmpty(file)) return "";
        string shortPath = file.Replace("\\", "/");
        int idx = shortPath.LastIndexOf("/");
        return idx >= 0 ? $"{shortPath.Substring(idx + 1)}:{line}" : $"{file}:{line}";
    }

    private static void Write(string level, string tag, string message, string src)
    {
        if (_logPath == null) return;

        string ts = DateTime.Now.ToString("HH:mm:ss.fff");
        string line = string.IsNullOrEmpty(src)
            ? $"[{ts}] {level,-5} [{tag}] {message}"
            : $"[{ts}] {level,-5} [{tag}] {message} ({src})";

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logPath, line + System.Environment.NewLine);
                AppendJsonl(level, tag, message, src);
            }
            catch (System.Exception ex)
            {
                // Fall back to GD.PrintErr so logging failures are never invisible.
                // File I/O failure (permissions, disk full) shouldn't crash the game.
                GD.PrintErr($"[GameLogger] Write failed: {ex.GetType().Name} — {ex.Message}");
            }
        }
    }

    private static void AppendJsonl(string level, string tag, string message, string src)
    {
        if (_jsonlPath == null) return;

        var entry = new Dictionary<string, object>
        {
            ["ts"] = DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fff"),
            ["level"] = level,
            ["tag"] = tag,
            ["msg"] = message,
            ["src"] = src,
            ["session"] = _sessionId,
        };

        File.AppendAllText(_jsonlPath, JsonSerializer.Serialize(entry) + System.Environment.NewLine);
    }

    /// <summary>Write to file only — no GD.Print echo. Used by GameLogBridge.</summary>
    internal static void LogToFile(string level, string tag, string message)
        => Write(level, tag, message, "");

    private static void RotateOldLogs()
    {
        if (!DirAccess.DirExistsAbsolute(_logDir)) return;

        var files = new List<string>();
        using var dir = DirAccess.Open(_logDir);
        if (dir == null) return;

        dir.ListDirBegin();
        string fileName;
        while ((fileName = dir.GetNext()) != "")
        {
            if (fileName.StartsWith(LogPrefix) &&
                (fileName.EndsWith(".log") || fileName.EndsWith(".jsonl")))
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

    /// <summary>Path to current JSONL mirror, or null if not initialized.</summary>
    public static string CurrentJsonlPath => _jsonlPath;

    /// <summary>Session id for the current boot (EGGBERT_SESSION_ID override, else generated).</summary>
    public static string CurrentSessionId => _sessionId;
}
