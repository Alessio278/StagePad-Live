using System.Text.Json;
namespace StagePad.App;
internal sealed record UiPreferences(string Language="it",bool FollowTriggeredSlot=true,string? ProjectDirectory=null)
{
    public static UiPreferences Parse(string json)
    {
        using var document=JsonDocument.Parse(json);var root=document.RootElement;
        string language="it";bool follow=true;
        if(root.TryGetProperty("language",out var value) && value.ValueKind==JsonValueKind.String &&
            value.GetString() is {} code && LocalizationCatalog.Languages.Contains(code))language=code;
        if(root.TryGetProperty("followTriggeredSlot",out var enabled) && enabled.ValueKind is JsonValueKind.True or JsonValueKind.False)follow=enabled.GetBoolean();
        string? directory=root.TryGetProperty("projectDirectory",out var folder) && folder.ValueKind==JsonValueKind.String?folder.GetString():null;
        return new(language,follow,string.IsNullOrWhiteSpace(directory)?null:directory);
    }
    public string ToJson()=>JsonSerializer.Serialize(new{language=Language,followTriggeredSlot=FollowTriggeredSlot,projectDirectory=ProjectDirectory});
}
