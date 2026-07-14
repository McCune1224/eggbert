using System;
using System.IO;
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
    private static bool _initialized;

    private const int MaxLogFiles = 5;
    private const string LogPrefix = "eggbert_";

    /// <summary>Call once at startup — from boot/GameInit.cs or an autoload _Ready.</summary>
    public static void Initialize(Level minLevel = Level.Info, bool echoToConsole = true)
    {
        if (_initialized) return;
        _initialized = true;

        _echoToConsole = echoToConsole;
        _minLevel = minLevel;
        _logDir = ProjectSettings.GlobalizePath("user://logs");
        Directory.CreateDirectory(_logDir);

        string today = DateTime.Now.ToString("yyyy-MM-dd");
        _logPath = Path.Combine(_logDir, $"{LogPrefix}{today}.log");
        RotateOldLogs();

        Info("GameLogger", $"Logging to {_logPath} (level: {_minLevel})");
        OS.AddLogger(new GameLogBridge());
    }

    /// <summary>Read EGGBERT_LOG_LEVEL and EGGBERT_LOG_ECHO env vars, then initialize.</summary>
    public static void InitializeFromEnv()
    {
        var level = Level.Info;
        var levelStr = System.Environment.GetEnvironmentVariable("EGGBERT_LOG_LEVEL");
        if (!string.IsNullOrEmpty(levelStr))
            Enum.TryParse(levelStr, ignoreCase: true, out level);

        var echo = true;
        var echoStr = System.Environment.GetEnvironmentVariable("EGGBERT_LOG_ECHO");
        if (echoStr == "0") echo = false;

        Initialize(level, echo);
    }

    public static void Debug(string tag, string message)
    {
        if (_minLevel > Level.Debug) return;
        Write("DEBUG", tag, message);
        if (_echoToConsole) GD.Print($"[{tag}] {message}");
    }

    public static void Info(string tag, string message)
    {
        if (_minLevel > Level.Info) return;
        Write("INFO", tag, message);
        if (_echoToConsole) GD.Print($"[{tag}] {message}");
    }

    public static void Warn(string tag, string message)
    {
        if (_minLevel > Level.Warn) return;
        Write("WARN", tag, message);
        if (_echoToConsole) GD.PushWarning($"[{tag}] {message}");
    }

    public static void Error(string tag, string message)
    {
        if (_minLevel > Level.Error) return;
        Write("ERROR", tag, message);
        if (_echoToConsole) GD.PrintErr($"[{tag}] {message}");
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
            catch
            {
                // Never let logging crash the game.
            }
        }
    }

    /// <summary>Write to file only — no GD.Print echo. Used by GameLogBridge.</summary>
    internal static void LogToFile(string level, string tag, string message) => Write(level, tag, message);

    private static void RotateOldLogs()
    {
        try
        {
            var files = Directory.GetFiles(_logDir, $"{LogPrefix}*.log");
            Array.Sort(files); // oldest first by name
            while (files.Length > MaxLogFiles)
            {
                File.Delete(files[0]);
                files = Directory.GetFiles(_logDir, $"{LogPrefix}*.log");
                Array.Sort(files);
            }
        }
        catch
        {
            // Best-effort rotation.
        }
    }

    /// <summary>Path to current log file, or null if not initialized.</summary>
    public static string CurrentLogPath => _logPath;
}
