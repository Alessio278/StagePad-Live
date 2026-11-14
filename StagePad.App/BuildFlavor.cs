namespace StagePad.App;
internal static class BuildFlavor
{
    public const string ProcessPrefix="StagePad";
    public const string ProductPrefix="StagePad Live";
    public static string MutexName=>"Local\\StagePad-Phase1-"+Environment.UserName;
}
