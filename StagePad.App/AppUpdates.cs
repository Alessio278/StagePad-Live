using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace StagePad.App;
internal sealed record UpdateRelease(Version Version,string Download,string Sha256);
internal sealed record UpdateCandidate(string Path,string Sha256,Version Version);
internal sealed record PreviousApp(string Target,string Version,string Sha256);
internal sealed class AppUpdates
{
    public const string Api="https://api.github.com/repos/Alessio278/StagePad-Live/releases/latest";
    private readonly HttpClient http;
    public string Root {get;}
    public static Version Current=>typeof(AppUpdates).Assembly.GetName().Version??new(0,0);
    public AppUpdates(HttpClient? client=null,string? root=null)
    {
        Root=root??Path.Combine(Log.Root,"updates");http=client??new HttpClient{Timeout=TimeSpan.FromMinutes(3)};
        http.DefaultRequestHeaders.UserAgent.ParseAdd("StagePad/"+Current);http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }
    private static string AssetUrl(string value)
    {
        if(!Uri.TryCreate(value,UriKind.Absolute,out var uri) || uri.Scheme!="https" || !uri.Host.Equals("github.com",StringComparison.OrdinalIgnoreCase) || !uri.AbsolutePath.StartsWith("/Alessio278/StagePad-Live/releases/download/",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Origine aggiornamento non valida.");
        return value;
    }
    public async Task<UpdateRelease?> Check(Version? current=null,CancellationToken ct=default)
    {
        using var reply=await http.GetAsync(Api,HttpCompletionOption.ResponseHeadersRead,ct);
        if(reply.StatusCode==HttpStatusCode.NotFound)return null;
        reply.EnsureSuccessStatusCode();using var data=JsonDocument.Parse(await Read(reply.Content,1024*1024,ct));var json=data.RootElement;
        if(json.GetProperty("draft").GetBoolean() || json.GetProperty("prerelease").GetBoolean())return null;
        string tag=json.GetProperty("tag_name").GetString()??"";
        if(!Version.TryParse(tag.TrimStart('v','V'),out var version) || version<=(current??Current))return null;
        JsonElement executable=default,checksum=default;
        foreach(var asset in json.GetProperty("assets").EnumerateArray())
        {if(asset.GetProperty("name").GetString()=="StagePad.exe")executable=asset;if(asset.GetProperty("name").GetString()=="SHA256.txt")checksum=asset;}
        if(executable.ValueKind==JsonValueKind.Undefined)throw new InvalidDataException("La release non contiene StagePad.exe.");
        string download=AssetUrl(executable.GetProperty("browser_download_url").GetString()!);
        string hash=executable.TryGetProperty("digest",out var digest)?digest.GetString()??"":"";
        if(hash.StartsWith("sha256:",StringComparison.Ordinal))hash=hash[7..];else hash="";
        if(!Regex.IsMatch(hash,"^[a-fA-F0-9]{64}$"))
        {
            if(checksum.ValueKind==JsonValueKind.Undefined)throw new InvalidDataException("Checksum aggiornamento mancante.");
            using var hashes=await http.GetAsync(AssetUrl(checksum.GetProperty("browser_download_url").GetString()!),ct);hashes.EnsureSuccessStatusCode();
            string text=System.Text.Encoding.UTF8.GetString(await Read(hashes.Content,65536,ct));
            var match=Regex.Match(text,@"(?im)^([a-f0-9]{64})\s+\*?StagePad\.exe\s*$");if(!match.Success)throw new InvalidDataException("Checksum aggiornamento mancante.");hash=match.Groups[1].Value;
        }
        return new(version,download,hash.ToLowerInvariant());
    }
    public async Task<UpdateCandidate> Download(UpdateRelease release,CancellationToken ct=default)
    {
        Directory.CreateDirectory(Root);string target=Path.Combine(Root,"candidate-"+release.Version+".exe"),temp=target+".tmp";
        if(File.Exists(target) && UpdateFiles.Hash(target)==release.Sha256){ValidateBinary(target,release.Version);return new(target,release.Sha256,release.Version);}
        try
        {
            using var reply=await http.GetAsync(AssetUrl(release.Download),HttpCompletionOption.ResponseHeadersRead,ct);reply.EnsureSuccessStatusCode();
            if(reply.Content.Headers.ContentLength is >268435456)throw new InvalidDataException("Aggiornamento troppo grande.");
            using(var input=await reply.Content.ReadAsStreamAsync(ct))using(var output=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,65536,true))
            {byte[] buffer=new byte[65536];long total=0;int size;while((size=await input.ReadAsync(buffer,ct))>0){total+=size;if(total>268435456)throw new InvalidDataException("Aggiornamento troppo grande.");await output.WriteAsync(buffer.AsMemory(0,size),ct);}}
            if(UpdateFiles.Hash(temp)!=release.Sha256)throw new InvalidDataException("Checksum aggiornamento non corrispondente.");
            ValidateBinary(temp,release.Version);File.Move(temp,target,true);Prune();return new(target,release.Sha256,release.Version);
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public PreviousApp? Previous(string target)
    {
        try{var record=JsonSerializer.Deserialize<PreviousApp>(File.ReadAllText(Path.Combine(Root,"previous.json")));return record!=null && Path.GetFullPath(record.Target).Equals(Path.GetFullPath(target),StringComparison.OrdinalIgnoreCase) && File.Exists(Path.Combine(Root,"previous.exe")) && UpdateFiles.Hash(Path.Combine(Root,"previous.exe"))==record.Sha256?record:null;}catch{return null;}
    }
    public UpdateCandidate StagePrevious(string target)
    {
        var previous=Previous(target)??throw new InvalidDataException("Versione precedente non disponibile.");
        string path=Path.Combine(Root,"rollback-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(Path.Combine(Root,"previous.exe"),path);return new(path,previous.Sha256,Version.Parse(previous.Version));
    }
    public static void ValidateBinary(string path,Version expected)
    {
        using var input=File.OpenRead(path);using var pe=new PEReader(input);
        if(pe.PEHeaders.CoffHeader.Machine!=Machine.Amd64 || pe.PEHeaders.PEHeader?.Subsystem!=Subsystem.WindowsGui)throw new InvalidDataException("Eseguibile aggiornamento non valido.");
        var info=FileVersionInfo.GetVersionInfo(path);
        if(info.ProductName?.StartsWith("StagePad Live",StringComparison.Ordinal)!=true || !Version.TryParse((info.ProductVersion??"").Split('+')[0],out var actual) || actual.Major!=expected.Major || actual.Minor!=expected.Minor || actual.Build!=expected.Build)throw new InvalidDataException("Versione o prodotto aggiornamento non validi.");
    }
    private static async Task<byte[]> Read(HttpContent content,int limit,CancellationToken ct)
    {using var input=await content.ReadAsStreamAsync(ct);using var output=new MemoryStream();byte[] buffer=new byte[8192];int count;while((count=await input.ReadAsync(buffer,ct))>0){if(output.Length+count>limit)throw new InvalidDataException("Risposta aggiornamento troppo grande.");output.Write(buffer,0,count);}return output.ToArray();}
    public void Prune()
    {
        foreach(string pattern in new[]{"candidate-*.exe","rollback-*.exe","update-runner-*.exe"})
            foreach(var file in new DirectoryInfo(Root).GetFiles(pattern).OrderByDescending(f=>f.LastWriteTimeUtc).Skip(3))try{file.Delete();}catch{}
    }
}
internal static class UpdateFiles
{
    public static string Hash(string path){using var file=File.OpenRead(path);return Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();}
    public static string Replace(string target,string candidate,string expectedHash)
    {
        if(Hash(candidate)!=expectedHash)throw new InvalidDataException("Checksum aggiornamento non corrispondente.");
        string temporary=target+".update-"+Guid.NewGuid().ToString("N"),backup=target+".previous-"+Guid.NewGuid().ToString("N");
        try{File.Copy(candidate,temporary);if(Hash(temporary)!=expectedHash)throw new InvalidDataException("Checksum aggiornamento non corrispondente.");File.Replace(temporary,target,backup,true);return backup;}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    public static void Restore(string target,string backup)
    {string temporary=target+".restore-"+Guid.NewGuid().ToString("N");try{File.Copy(backup,temporary);File.Move(temporary,target,true);}finally{if(File.Exists(temporary))File.Delete(temporary);}}
}
