using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Abstractions;
using static Crayon.Output;

namespace systray_doom;

/// <summary>
/// A custom console formatter that uses Crayon for coloring and outputs logs in a single line.
/// </summary>
public sealed class CrayonConsoleFormatter : ConsoleFormatter
{
    public CrayonConsoleFormatter() : base(nameof(CrayonConsoleFormatter))
    {
    }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        var logLevel = logEntry.LogLevel;
        var categoryName = logEntry.Category;
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

        // Apply colors based on log level
        var levelText = GetLogLevelText(logLevel);
        var coloredLevel = ApplyLogLevelColor(levelText, logLevel);
        var coloredMessage = ApplyMessageColor(message, logLevel);

        // Format: [HH:mm:ss.fff] LEVEL Category: Message
        textWriter.WriteLine($"{Dim($"[{timestamp}]")} {coloredLevel} {Dim($"{categoryName}:")} {coloredMessage}");

        // Write exception if present
        if (logEntry.Exception != null)
        {
            textWriter.WriteLine(Red(logEntry.Exception.ToString()));
        }
    }

    private static string GetLogLevelText(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "TRCE",
        LogLevel.Debug => "DBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARN",
        LogLevel.Error => "ERRO",
        LogLevel.Critical => "CRIT",
        LogLevel.None => "NONE",
        _ => "UNKN"
    };

    private static string ApplyLogLevelColor(string levelText, LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => Dim(levelText),
        LogLevel.Debug => Dim(levelText),
        LogLevel.Information => levelText,
        LogLevel.Warning => Yellow(levelText),
        LogLevel.Error => Red(levelText),
        LogLevel.Critical => Bold(Red(levelText)),
        _ => levelText
    };

    private static string ApplyMessageColor(string message, LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => Dim(message),
        LogLevel.Debug => Dim(message),
        LogLevel.Information => message,
        LogLevel.Warning => Yellow(message),
        LogLevel.Error => Red(message),
        LogLevel.Critical => Bold(Red(message)),
        _ => message
    };
}
