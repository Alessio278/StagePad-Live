using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace StagePad.App;
internal static class Notices
{
    public static async Task Open()
    {
        string folder = System.IO.Path.Combine(Log.Root, "licenses");
        await Task.Run(() =>
        {
            Directory.CreateDirectory(folder);
            var assembly = Assembly.GetExecutingAssembly();
            foreach (string name in assembly.GetManifestResourceNames().Where(x => x.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
            {
                using var input = assembly.GetManifestResourceStream(name)!;
                using var output = File.Create(System.IO.Path.Combine(folder, name)); input.CopyTo(output);
            }
        });
        Process.Start(new ProcessStartInfo("explorer.exe", folder) { UseShellExecute = true });
    }
}
