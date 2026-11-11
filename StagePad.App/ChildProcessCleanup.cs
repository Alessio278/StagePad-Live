using System.Diagnostics;
namespace StagePad.App;
internal static class ChildProcessCleanup
{
    public static void StopAll(IEnumerable<Process> children,int graceMilliseconds=1800)
    {
        var processes=children.ToArray();long deadline=Environment.TickCount64+graceMilliseconds;
        foreach(var child in processes)
        {
            try{if(!child.HasExited)child.WaitForExit((int)Math.Max(0,deadline-Environment.TickCount64));}
            catch(Exception ex){Log.Warn("shutdown-wait",ex.Message);}
        }
        // Kill all survivors first, so a slow driver does not delay the others.
        foreach(var child in processes)
            try{if(!child.HasExited)child.Kill();}catch(Exception ex){Log.Warn("shutdown-kill",ex.Message);}
        foreach(var child in processes)
            try{if(!child.HasExited)child.WaitForExit(500);}catch(Exception ex){Log.Warn("shutdown-exit",ex.Message);}
    }
}
