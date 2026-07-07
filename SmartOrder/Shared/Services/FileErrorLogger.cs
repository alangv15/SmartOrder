using System.Text;

namespace SmartOrder.Shared.Services;

public static class FileErrorLogger
{
    private static readonly object SyncRoot = new();

    public static string LogFilePath => Path.Combine(AppContext.BaseDirectory, "smartorder-errors.log");

    public static void Log(string source, Exception exception, string? message = null)
    {
        try
        {
            var builder = new StringBuilder();
            builder.AppendLine("==================================================");
            builder.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"Source: {source}");

            if (!string.IsNullOrWhiteSpace(message))
            {
                builder.AppendLine($"Message: {message}");
            }

            AppendException(builder, exception, 0);
            builder.AppendLine();

            lock (SyncRoot)
            {
                File.AppendAllText(LogFilePath, builder.ToString());
            }
        }
        catch
        {
        }
    }

    public static void LogMessage(string source, string message)
    {
        try
        {
            var content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}: {message}{Environment.NewLine}";
            lock (SyncRoot)
            {
                File.AppendAllText(LogFilePath, content);
            }
        }
        catch
        {
        }
    }

    private static void AppendException(StringBuilder builder, Exception exception, int level)
    {
        var prefix = level == 0 ? "Exception" : $"InnerException {level}";
        builder.AppendLine($"{prefix} Type: {exception.GetType().FullName}");
        builder.AppendLine($"{prefix} Message: {exception.Message}");
        builder.AppendLine($"{prefix} StackTrace:");
        builder.AppendLine(exception.StackTrace ?? "(sin stack trace)");

        if (exception.InnerException != null)
        {
            AppendException(builder, exception.InnerException, level + 1);
        }
    }
}

