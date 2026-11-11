using System.IO;
namespace StagePad.App;
internal static class ProjectDirectoryPolicy
{
    public static string FileName(string value)
    {
        string name=value.Trim();
        if(name.EndsWith(".json",StringComparison.OrdinalIgnoreCase))name=name[..^5];
        if(string.IsNullOrWhiteSpace(name) || name.Length>100 || name.EndsWith('.') ||
           name.IndexOfAny(['<','>',':','"','/','\\','|','?','*'])>=0 || name.Any(char.IsControl) || name is "." or "..")
            throw new InvalidDataException("Nome progetto non valido. Usa un nome senza caratteri speciali o percorsi.");
        string stem=name.Split('.')[0];
        if(new[]{"CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9","LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"}.Contains(stem,StringComparer.OrdinalIgnoreCase))
            throw new InvalidDataException("Nome progetto non valido. Usa un nome senza caratteri speciali o percorsi.");
        if(name.Equals("last-project",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Nome riservato al recupero automatico. Scegli un altro nome.");
        return name+".json";
    }
    public static string DirectoryPath(string value)
    {
        string path=Environment.ExpandEnvironmentVariables(value.Trim());
        if(string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            throw new InvalidDataException("Inserisci il percorso completo della cartella.");
        return Path.GetFullPath(path);
    }
}
