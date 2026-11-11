using System.Reflection;
using System.Text.Json;
namespace StagePad.App;
internal static class LocalizationCatalog
{
    public static readonly string[] Languages=["it","en","es","fr"];
    public static readonly IReadOnlyDictionary<string,Dictionary<string,string>> Entries=Load();
    private static Dictionary<string,Dictionary<string,string>> Load()
    {
        using var stream=typeof(LocalizationCatalog).Assembly.GetManifestResourceStream("StagePad.Localization.json")
            ??throw new InvalidOperationException("Missing localization resource.");
        return JsonSerializer.Deserialize<Dictionary<string,Dictionary<string,string>>>(stream)!;
    }
    public static bool Has(string key)=>Entries.ContainsKey(key);
    public static string Translate(string language,string text)
    {
        if(language=="it" || string.IsNullOrEmpty(text))return text;
        if(Entries.TryGetValue(text,out var item) && item.TryGetValue(language,out var translated))return translated;
        return text;
    }
    public static string Message(string language,string text)
    {
        string translated=Translate(language,text);if(translated!=text)return translated;
        // Translate known diagnostics without touching user names or paths.
        foreach(var prefix in new[]{"Salvataggio non riuscito: ","Ripristino precedente non leggibile: ","Spostamento non riuscito: ","Motore non raggiungibile: ","Non disponibile · ","Audio pronto · ","Ripristino automatico non salvato: ","MIDI non disponibile: "})
            if(text.StartsWith(prefix,StringComparison.Ordinal))return Translate(language,prefix)+text[prefix.Length..];
        return string.Join(" · ",text.Split(" · ").Select(part=>Translate(language,part)));
    }
}
