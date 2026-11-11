namespace StagePad.App;
internal static class LifecycleSignals
{
    internal static string Name(string suffix="")=>"Local\\StagePad-Close-"+Environment.UserName+suffix;
    public static EventWaitHandle Create(string suffix="")
    {var signal=new EventWaitHandle(false,EventResetMode.ManualReset,Name(suffix));signal.Reset();return signal;}
    public static bool RequestClose(string suffix="")
    {
        try{using var signal=EventWaitHandle.OpenExisting(Name(suffix));return signal.Set();}
        catch(Exception ex){Log.Warn("close-signal",ex.Message);return false;}
    }
}
