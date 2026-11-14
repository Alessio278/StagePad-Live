using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using StagePad.Core;
namespace StagePad.App;

internal static class LegacyDataMigration
{
    public static void Run(string root)
    {
        root=Path.GetFullPath(root);
        string legacy=Path.Combine(root,"preview-markers"),backup=Path.Combine(root,"backups","legacy-data");
        if(Directory.Exists(legacy))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
            if(Directory.Exists(backup) && File.Exists(Path.Combine(backup,"migration.complete")))backup+="-"+Guid.NewGuid().ToString("N");
            if(Directory.Exists(backup))throw new IOException("Completa prima il recupero precedente dei progetti.");
            Directory.Move(legacy,backup);
        }
        if(!Directory.Exists(backup) || File.Exists(Path.Combine(backup,"migration.complete"))){NormalizeSettings(root,legacy,null);return;}
        string projectFolder=Path.Combine(root,"project");Directory.CreateDirectory(projectFolder);
        string mapPath=Path.Combine(backup,"migration-map.json");
        var map=new Dictionary<string,string>(File.Exists(mapPath)?JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(mapPath))??[]:[],StringComparer.OrdinalIgnoreCase);
        string Remap(string path)=>Inside(path,legacy)?Path.Combine(backup,Path.GetRelativePath(legacy,path)):path;
        ProjectDocument RemapProject(ProjectDocument project)=>project with{Slots=project.Slots.Select(s=>s with{File=s.File==null?null:Remap(s.File)}).ToArray()};
        string oldProjects=Path.Combine(backup,"project");
        if(Directory.Exists(oldProjects))foreach(string file in Directory.EnumerateFiles(oldProjects,"*.json").Take(5000))
        {
            if(Path.GetFileName(file).Equals("last-project.json",StringComparison.OrdinalIgnoreCase))continue;
            if(map.TryGetValue(file,out var existing) && File.Exists(existing) && Inside(existing,projectFolder))continue;
            try
            {
                var project=RemapProject(ProjectFiles.Read(file));string stem=Path.GetFileNameWithoutExtension(file);
                string target=Path.Combine(projectFolder,ProjectDirectoryPolicy.FileName(stem));
                if(File.Exists(target))
                {
                    int n=1;string safeStem=stem.Length>75?stem[..75]:stem;
                    do{target=Path.Combine(projectFolder,ProjectDirectoryPolicy.FileName(safeStem+" - recuperato "+n++));}while(File.Exists(target));
                }
                ProjectFiles.Write(target,project,false);map[file]=target;AtomicWrite(mapPath,JsonSerializer.Serialize(map));
            }
            catch(Exception ex){Log.Warn("migration-project",Path.GetFileName(file)+": "+ex.Message);}
        }
        string recovery=Path.Combine(oldProjects,"last-project.json"),destination=Path.Combine(projectFolder,"last-project.json");
        if(File.Exists(recovery) && (!File.Exists(destination) || File.GetLastWriteTimeUtc(recovery)>File.GetLastWriteTimeUtc(destination)))
        {
            try
            {
            var project=RemapProject(ProjectFiles.Read(recovery));using var document=JsonDocument.Parse(File.ReadAllText(recovery));
            string? active=null;
            if(document.RootElement.TryGetProperty("lastProjectPath",out var path) && path.ValueKind==JsonValueKind.String && path.GetString() is {} stored)
            {string remapped=Remap(stored);active=map.TryGetValue(remapped,out var saved)?saved:File.Exists(remapped)?remapped:null;}
            ProjectFiles.Write(destination,project,false,true,active);
            }
            catch(Exception ex){Log.Warn("migration-recovery",ex.Message);}
        }
        NormalizeSettings(root,legacy,Path.Combine(backup,"settings.json"));
        AtomicWrite(Path.Combine(backup,"migration.complete"),DateTimeOffset.UtcNow.ToString("O"));
    }
    private static void NormalizeSettings(string root,string legacy,string? oldSettings)
    {
        string settings=Path.Combine(root,"settings.json");
        if(!File.Exists(settings) && (oldSettings==null || !File.Exists(oldSettings)))return;
        try
        {
            var json=JsonNode.Parse(File.ReadAllText(File.Exists(settings)?settings:oldSettings!)) as JsonObject??new();
            if(json["projectDirectory"] is JsonValue value && value.TryGetValue<string>(out var directory) && Inside(directory,legacy))json["projectDirectory"]=null;
            AtomicWrite(settings,json.ToJsonString());
        }
        catch(Exception ex){Log.Warn("migration-settings",ex.Message);}
    }
    private static bool Inside(string path,string root)
    {
        if(!Path.IsPathFullyQualified(path))return false;
        string full=Path.GetFullPath(path),prefix=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
        return full.Equals(prefix,StringComparison.OrdinalIgnoreCase) || full.StartsWith(prefix+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
    }
    private static void AtomicWrite(string path,string text)
    {
        string temp=path+".migration-tmp";
        try{File.WriteAllText(temp,text);File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
