using System.Collections.Concurrent;
using System.IO;

namespace StagePad.App;
internal static class Log
{
    private static readonly BlockingCollection<string> Queue = new(1024);
    private static int started, unavailable;
    public static string Root => Environment.GetEnvironmentVariable("STAGEPAD_DATA_DIR") ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StagePad");
    public static bool Unavailable => Volatile.Read(ref unavailable) != 0;
    public static void Start(string role)
    {
        if (Interlocked.Exchange(ref started, 1) != 0) return;
        new Thread(() => Run(role)) { IsBackground = true, Name = "StagePad log" }.Start();
    }
    public static void Warn(string area, string message) => Write(area, message);
    public static void Error(string area, Exception ex) => Write(area, ex.ToString());
    private static void Write(string area, string message)
    {
        // No caller on the audio callback. The queue is bounded; a full disk never blocks callers.
        string line = $"{DateTimeOffset.Now:O} [{area}] {message}";
        Queue.TryAdd(line.Length > 12000 ? line[..12000] : line);
    }
    private static void Run(string role)
    {
        string file = System.IO.Path.Combine(Root, "logs", $"{role}-{Environment.ProcessId}.log");
        foreach (var line in Queue.GetConsumingEnumerable())
        {
            try
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
                if (File.Exists(file) && new FileInfo(file).Length > 2 * 1024 * 1024)
                {
                    for (int i = 3; i >= 1; i--)
                    {
                        string from = i == 1 ? file : file + "." + (i - 1);
                        if (File.Exists(from)) File.Move(from, file + "." + i, true);
                    }
                }
                File.AppendAllText(file, line + Environment.NewLine);
                Volatile.Write(ref unavailable, 0);
                // Bound logs across process restarts as well as per-process rotation.
                foreach (var old in new DirectoryInfo(System.IO.Path.GetDirectoryName(file)!).GetFiles("*.log*").OrderByDescending(x => x.LastWriteTimeUtc).Skip(24))
                    try { old.Delete(); } catch { }
            }
            catch { Volatile.Write(ref unavailable, 1); }
        }
    }
}
