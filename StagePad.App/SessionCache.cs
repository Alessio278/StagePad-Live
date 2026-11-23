using System.Diagnostics;
using System.IO;

namespace StagePad.App;
internal static class SessionCache
{
    public static void Prune(string root)
    {
        if (!Directory.Exists(root) || File.GetAttributes(root).HasFlag(FileAttributes.ReparsePoint)) return;
        foreach (var folder in Directory.EnumerateDirectories(root))
        {
            try
            {
                var owner = Path.GetFileName(folder).Split('-');
                if (owner.Length != 2 || !Guid.TryParseExact(owner[1], "N", out _) ||
                    !int.TryParse(owner[0], out int pid) || pid <= 0 ||
                    File.GetAttributes(folder).HasFlag(FileAttributes.ReparsePoint)) continue;
                try { using var process = Process.GetProcessById(pid); if (!process.HasExited) continue; }
                catch (ArgumentException) { }
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    string name = Path.GetFileName(file);
                    if (name.Length < 36 || !Guid.TryParseExact(name[..32], "N", out _) ||
                        name[32..] is not (".pcm" or ".pcm.wave" or ".pcm.error") ||
                        File.GetAttributes(file).HasFlag(FileAttributes.ReparsePoint)) continue;
                    File.Delete(file);
                }
                if (!Directory.EnumerateFileSystemEntries(folder).Any()) Directory.Delete(folder);
            }
            catch (Exception ex) { Log.Warn("cache-prune", ex.Message); }
        }
    }
}
