namespace ADB_Explorer.Services;

public static class DebugLog
{
    private static readonly Mutex mutex = new();

    public static void PrintLine(string message)
    {
        mutex.WaitOne();

        try
        {
            if (string.IsNullOrEmpty(Properties.AppGlobal.DragDropLogPath))
                return;

            var logPath = Environment.ExpandEnvironmentVariables(Properties.AppGlobal.DragDropLogPath);
            var logDir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            File.AppendAllText(logPath, $"{DateTime.Now:HH:mm:ss:fff} | {message}\n");
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}
