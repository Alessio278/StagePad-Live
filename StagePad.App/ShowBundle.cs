using System.IO;
using System.IO.Compression;
using StagePad.Core;
namespace StagePad.App;
internal static class ShowBundle
{
    private const long MaximumBytes=8L*1024*1024*1024;
    public static void Export(string destination,ProjectDocument project,CancellationToken ct=default)
    {
        Projects.Validate(project);destination=Path.GetFullPath(destination);
        if(File.Exists(destination))throw new IOException("Il pacchetto esiste già: scegli un altro nome.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string temporary=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            var media=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);long total=0;
            using(var zip=ZipFile.Open(temporary,ZipArchiveMode.Create))
            {
                var slots=project.Slots.Select(s=>
                {
                    ct.ThrowIfCancellationRequested();if(string.IsNullOrWhiteSpace(s.File))return s;
                    string file=Path.GetFullPath(s.File);
                    if(!media.TryGetValue(file,out var relative))
                    {
                        var info=new FileInfo(file);if(!info.Exists)throw new FileNotFoundException("Audio mancante: "+file);
                        total=checked(total+info.Length);if(total>MaximumBytes)throw new InvalidDataException("Pacchetto troppo grande: massimo 8 GiB.");
                        string basename=Path.GetFileName(file);if(basename.IndexOfAny(['/', '\\',':'])>=0)throw new InvalidDataException("Nome audio non valido.");
                        relative=$"audio/{media.Count+1:000}/{basename}";media[file]=relative;
                        var entry=zip.CreateEntry(relative,CompressionLevel.Fastest);
                        using var input=File.OpenRead(file);using var output=entry.Open();Copy(input,output,info.Length,ct);
                    }
                    return s with{File=relative};
                }).ToArray();
                var json=zip.CreateEntry("project.json");using var writer=new StreamWriter(json.Open());
                writer.Write(System.Text.Json.JsonSerializer.Serialize(project with{Slots=slots},Projects.Json));
            }
            ct.ThrowIfCancellationRequested();File.Move(temporary,destination,false);
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    public static string Import(string archive,string directory,string name,CancellationToken ct=default)
    {
        directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);
        string fileName=ProjectDirectoryPolicy.FileName(name),destination=Path.Combine(directory,fileName);
        if(File.Exists(destination))throw new IOException("Il progetto esiste già: scegli un altro nome.");
        string id=Guid.NewGuid().ToString("N"),stage=Path.Combine(directory,".import-"+id),assets=Path.Combine(directory,Path.GetFileNameWithoutExtension(fileName)+".audio-"+id[..8]);
        string jsonTemporary=Path.Combine(directory,".import-"+id+".json");bool moved=false,committed=false;
        Directory.CreateDirectory(stage);
        try
        {
            using(var zip=ZipFile.OpenRead(archive))
            {
                if(zip.Entries.Count>256)throw new InvalidDataException("Pacchetto non valido: troppi file.");
                long total=0;var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach(var entry in zip.Entries)
                {
                    ct.ThrowIfCancellationRequested();string relative=entry.FullName.Replace('\\','/');
                    if(relative.EndsWith('/'))continue;
                    if(relative.StartsWith('/') || relative.Contains(':') || relative.Split('/').Any(p=>p is "." or ".." or "") || ((entry.ExternalAttributes>>16)&0xF000)==0xA000 || !seen.Add(relative))throw new InvalidDataException("Percorso non valido nel pacchetto.");
                    if(relative!="project.json" && !relative.StartsWith("audio/",StringComparison.Ordinal))throw new InvalidDataException("Contenuto non valido nel pacchetto.");
                    total=checked(total+entry.Length);if(total>MaximumBytes || relative=="project.json" && entry.Length>4*1024*1024)throw new InvalidDataException("Pacchetto troppo grande: massimo 8 GiB.");
                    string target=Path.GetFullPath(relative,stage);if(!Inside(target,stage))throw new InvalidDataException("Percorso non valido nel pacchetto.");
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    using var input=entry.Open();using var output=new FileStream(target,FileMode.CreateNew);Copy(input,output,entry.Length,ct);
                }
            }
            var project=ProjectFiles.Read(Path.Combine(stage,"project.json"));
            foreach(var slot in project.Slots)if(slot.File is {} file && (!Inside(file,Path.Combine(stage,"audio")) || !File.Exists(file)))throw new InvalidDataException("Il pacchetto contiene riferimenti audio esterni o mancanti.");
            Directory.Move(stage,assets);moved=true;
            project=project with{Name=Path.GetFileNameWithoutExtension(fileName),Slots=project.Slots.Select(s=>s.File==null?s:s with{File=Path.Combine(assets,Path.GetRelativePath(stage,s.File))}).ToArray()};
            ProjectFiles.Write(jsonTemporary,project,true);File.Move(jsonTemporary,destination,false);committed=true;return destination;
        }
        finally
        {
            if(Directory.Exists(stage))Directory.Delete(stage,true);
            if(moved && !committed && Directory.Exists(assets))Directory.Delete(assets,true);
            if(File.Exists(jsonTemporary))File.Delete(jsonTemporary);
        }
    }
    private static bool Inside(string file,string directory)=>Path.GetFullPath(file).StartsWith(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
    private static void Copy(Stream input,Stream output,long expected,CancellationToken ct)
    {
        byte[] buffer=new byte[65536];long copied=0;int count;
        while((count=input.Read(buffer))>0){ct.ThrowIfCancellationRequested();copied=checked(copied+count);if(copied>expected)throw new InvalidDataException("Dimensione del pacchetto non valida.");output.Write(buffer,0,count);}
        if(copied!=expected)throw new InvalidDataException("Dimensione del pacchetto non valida.");
    }
}
