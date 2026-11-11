using System.IO;
namespace StagePad.App;
internal static class ProjectManagement
{
    public static void Validate(string source,string? target,string operation)
    {
        source=Path.GetFullPath(source);
        if(operation is not ("rename-project" or "duplicate-project" or "delete-project"))throw new InvalidDataException("Operazione non valida.");
        ProjectDirectoryPolicy.FileName(Path.GetFileName(source));
        if(!Path.GetExtension(source).Equals(".json",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Operazione non valida.");
        if(operation=="delete-project")return;
        if(string.IsNullOrWhiteSpace(target))throw new InvalidDataException("Nome progetto non valido. Usa un nome senza caratteri speciali o percorsi.");
        target=Path.GetFullPath(target);
        ProjectDirectoryPolicy.FileName(Path.GetFileName(target));
        if(!Path.GetExtension(target).Equals(".json",StringComparison.OrdinalIgnoreCase) ||
           !string.Equals(Path.GetDirectoryName(source),Path.GetDirectoryName(target),StringComparison.OrdinalIgnoreCase) ||
           string.Equals(source,target,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Scegli un nome diverso nella stessa cartella.");
    }
    public static void Execute(string source,string? target,string operation)
    {
        Validate(source,target,operation);
        if(!File.Exists(source))throw new FileNotFoundException("Progetto non trovato.");
        // Copy without overwriting; relative audio references remain valid in the same directory.
        if(operation=="duplicate-project")File.Copy(source,target!,false);
        else if(operation=="rename-project")File.Move(source,target!,false);
        else File.Delete(source);
    }
}
