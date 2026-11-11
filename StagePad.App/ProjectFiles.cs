using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using StagePad.Core;

namespace StagePad.App;
internal static class ProjectFiles
{
    public static string DefaultDirectory=>System.IO.Path.Combine(Log.Root,"project");
    public static string Recovery=>System.IO.Path.Combine(DefaultDirectory,"last-project.json");
    public static ProjectDocument ReadStartup(out string? activePath,out string? warning,string? dataRoot=null)
    {
        string root=dataRoot??Log.Root;activePath=null;warning=null;
        foreach(string path in RecoveryCandidates(root))
        {
            if(!File.Exists(path))continue;
            try
            {
                var project=Read(path);
                using var json=JsonDocument.Parse(File.ReadAllText(path));
                if(json.RootElement.TryGetProperty("lastProjectPath",out var stored) && stored.ValueKind==JsonValueKind.String && stored.GetString() is {} source)
                {
                    if(Path.IsPathFullyQualified(source) && File.Exists(source))activePath=Path.GetFullPath(source);
                    else warning="Ultimo show non trovato: ripristinata la copia di recupero.";
                }
                return project;
            }
            catch(Exception ex){warning="Ripristino precedente non leggibile: "+ex.Message;Log.Error("project-startup",ex);}
        }
        return new ProjectDocument();
    }
    private static string[] RecoveryCandidates(string root)
    {
        string current=Path.Combine(root,"project","last-project.json"),legacy=Path.Combine(root,"recovery","last-project.json");
        return [current,current+".bak",legacy,legacy+".bak"];
    }
    public static ProjectDocument ReadRecovery(out string? warning,string? dataRoot=null)
    {
        string root=dataRoot??Log.Root;warning=null;
        string current=System.IO.Path.Combine(root,"project","last-project.json");
        string legacy=System.IO.Path.Combine(root,"recovery","last-project.json");
        foreach(string path in new[]{current,current+".bak",legacy,legacy+".bak"})
        {
            if(!File.Exists(path))continue;
            try{return Read(path);}
            catch(Exception ex){warning="Ripristino precedente non leggibile: "+ex.Message;Log.Error("project-restore",ex);}
        }
        return new ProjectDocument();
    }
    public static ProjectDocument Read(string path)
    {
        if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("Progetto troppo grande.");
        var p = JsonSerializer.Deserialize<ProjectDocument>(File.ReadAllText(path), Projects.Json) ?? throw new InvalidDataException("Progetto vuoto.");
        if(p.Grid!=null && p.SetupGrid!=null && p.SlotCount is >=1 and <=Projects.MaxSlots)
            p=p with { Grid=Projects.FitGrid(p.Grid,p.SlotCount),SetupGrid=Projects.FitGrid(p.SetupGrid,p.SlotCount) };
        Projects.Validate(p);
        string folder = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path))!;
        return p with { Slots = p.Slots.Select(s => s with { File = string.IsNullOrWhiteSpace(s.File) ? null : System.IO.Path.GetFullPath(s.File, folder) }).ToArray() };
    }
    public static void Write(string path, ProjectDocument p, bool relative, bool session=false, string? activePath=null)
    {
        Projects.Validate(p); path = System.IO.Path.GetFullPath(path);
        string folder = System.IO.Path.GetDirectoryName(path)!; Directory.CreateDirectory(folder);
        if (relative) p = p with { Slots = p.Slots.Select(s => s with { File = s.File == null ? null : System.IO.Path.GetRelativePath(folder, s.File) }).ToArray() };
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 16384, FileOptions.WriteThrough))
            {
                if(session)
                {
                    var document=JsonSerializer.SerializeToNode(p,Projects.Json)!;
                    document["lastProjectPath"]=activePath;
                    using var writer=new Utf8JsonWriter(stream,new JsonWriterOptions {Indented=true});
                    document.WriteTo(writer,Projects.Json);writer.Flush();
                }
                else JsonSerializer.Serialize(stream,p,Projects.Json);
                stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak", true);
            else File.Move(temporary, path);
        }
        finally { try { File.Delete(temporary); } catch { } }
    }
}
internal sealed class RecoveryWriter : IDisposable
{
    private sealed record Session(ProjectDocument Project,string? Path);
    private Session? latest;
    private readonly CancellationTokenSource stop = new();
    private readonly Thread thread;
    private string? error;
    private long version, saved;
    public string? Error => Volatile.Read(ref error);
    public RecoveryWriter()
    {
        thread = new Thread(Run) { IsBackground = true, Name = "StagePad project recovery writer" }; thread.Start();
    }
    public void Schedule(ProjectDocument p,string? activePath=null) { Volatile.Write(ref latest, new Session(p,activePath)); Interlocked.Increment(ref version); }
    private void Save()
    {
        long stamp = Volatile.Read(ref version); var value = Volatile.Read(ref latest);
        if (value == null || stamp == saved) return;
        try { ProjectFiles.Write(ProjectFiles.Recovery, value.Project, false,true,value.Path); saved = stamp; Volatile.Write(ref error, null); }
        catch (Exception ex) { Log.Error("project-autosave", ex); Volatile.Write(ref error, "Ripristino automatico non salvato: " + ex.Message); }
    }
    private void Run()
    {
        while (!stop.Token.WaitHandle.WaitOne(300)) Save();
        Save();
    }
    public void Dispose() { stop.Cancel(); thread.Join(2000); }
}
internal sealed class MidiLink : IDisposable
{
    private readonly string pipe;
    private readonly BlockingCollection<Request> messages = new(256);
    private readonly CancellationTokenSource stop = new();
    private readonly Thread thread;
    private string? target, alert;
    private int configRevision, appliedRevision = -1;
    private long panic = -1, epoch;
    public string? Alert => Volatile.Read(ref alert);
    public MidiLink(string pipe)
    {
        this.pipe = pipe;
        thread = new Thread(Run) { IsBackground = true, Name = "StagePad MIDI IPC sender" }; thread.Start();
    }
    public void Configure(string? id) { Volatile.Write(ref target, id); Interlocked.Increment(ref configRevision); }
    public bool Post(Request request)
    {
        if (messages.TryAdd(request with { Epoch = Volatile.Read(ref epoch), Timestamp = Environment.TickCount64 })) return true;
        Volatile.Write(ref alert, "Coda MIDI piena: comando scartato."); return false;
    }
    public void Panic()
    {
        long next = Interlocked.Increment(ref epoch), current;
        do { current = Volatile.Read(ref panic); if (current >= next) return; }
        while (Interlocked.CompareExchange(ref panic, next, current) != current);
    }
    private void Send(Request r)
    {
        var reply = Wire.Call(pipe, r, 500).GetAwaiter().GetResult();
        if (!reply.Ok) throw new InvalidOperationException(reply.Error);
    }
    private void Run()
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                int revision = Volatile.Read(ref configRevision);
                if (revision != appliedRevision)
                { Send(new("configure", DeviceId: Volatile.Read(ref target))); appliedRevision = revision; Volatile.Write(ref alert, null); }
                long p = Interlocked.Exchange(ref panic, -1);
                if (p >= 0) Send(new("panic", Epoch: p));
                if (messages.TryTake(out var r, 5))
                {
                    if (r.Epoch < Volatile.Read(ref epoch)) continue;
                    if (r.Op == "send" && Environment.TickCount64 - r.Timestamp > 250)
                    { Volatile.Write(ref alert, "Messaggio MIDI scartato perché in ritardo."); continue; }
                    long urgent=Interlocked.Exchange(ref panic,-1);if(urgent>=0)Send(new("panic",Epoch:urgent));
                    if(r.Epoch<Volatile.Read(ref epoch))continue;
                    Send(r); Volatile.Write(ref alert, null);
                }
            }
            catch (Exception ex)
            {
                Volatile.Write(ref alert, "MIDI non disponibile: " + ex.Message); Log.Warn("midi-link", ex.Message);
                // Config can be retried; trigger commands are consumed once, never replayed.
                stop.Token.WaitHandle.WaitOne(250);
            }
        }
    }
    public void Dispose() { stop.Cancel(); thread.Join(1000); }
}
