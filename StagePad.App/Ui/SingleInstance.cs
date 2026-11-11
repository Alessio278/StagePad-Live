using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
namespace StagePad.App;
internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex mutex=new(false,"Local\\StagePad-Phase1-"+Environment.UserName);
    private bool owns;
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr window);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window,int command);
    [DllImport("advapi32.dll",SetLastError=true)] private static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr handle);
    private bool Take(int wait=0)
    {
        try{return owns=mutex.WaitOne(wait);}
        catch(AbandonedMutexException){return owns=true;}
    }
    public static SingleInstance? Enter()
    {
        var lease=new SingleInstance();if(lease.Take())return lease;
        var others=Candidates();
        try
        {
            if(Activate(others)){lease.Dispose();return null;}
            // Let an already-starting or normally-closing instance finish first.
            long deadline=Environment.TickCount64+5000;
            while(Environment.TickCount64<deadline)
            {
                if(lease.Take(100))return lease;
                if(Activate(others)){lease.Dispose();return null;}
            }
            foreach(var process in others)process.Dispose();others=Candidates();
            if(Activate(others)){lease.Dispose();return null;}
            foreach(var process in others)LifecycleSignals.RequestClose("-"+process.Id);if(lease.Take(2200))return lease;
            foreach(var process in others)process.Dispose();others=Candidates();
            if(Activate(others)){lease.Dispose();return null;}
            // Recovery is restricted to this product, Windows user and session,
            // with no visible StagePad window. Never stop a visible live instance.
            ChildProcessCleanup.StopAll(others,0);
            if(lease.Take(1800))return lease;
            Log.Warn("instance","Istanza precedente ancora in chiusura.");
            System.Windows.MessageBox.Show("StagePad è ancora in chiusura. Attendi qualche secondo e riprova.","StagePad");
            lease.Dispose();return null;
        }
        finally{foreach(var process in others)process.Dispose();}
    }
    private static Process[] Candidates()
    {
        var result=new List<Process>();using var self=Process.GetCurrentProcess();
        using var identity=WindowsIdentity.GetCurrent();string? sid=identity.User?.Value;
        foreach(var p in Process.GetProcesses())
        {
            bool keep=false;IntPtr token=IntPtr.Zero;
            try
            {
                if(p.Id==self.Id || p.SessionId!=self.SessionId)continue;
                if(!p.ProcessName.StartsWith("StagePad",StringComparison.OrdinalIgnoreCase))continue;
                string? file=p.MainModule?.FileName;
                if(file==null || FileVersionInfo.GetVersionInfo(file).ProductName?.StartsWith("StagePad Live",StringComparison.Ordinal)!=true)continue;
                if(!OpenProcessToken(p.Handle,8,out token))continue;
                using var owner=new WindowsIdentity(token);
                if(sid==null || owner.User?.Value!=sid)continue;
                keep=true;result.Add(p);
            }
            catch(Exception){/* Other users/protected processes are never candidates. */}
            finally{if(token!=IntPtr.Zero)CloseHandle(token);if(!keep)p.Dispose();}
        }
        return result.ToArray();
    }
    private static bool Activate(IEnumerable<Process> processes)
    {
        foreach(var p in processes)
        {
            try
            {
                p.Refresh();IntPtr window=p.MainWindowHandle;
                if(window==IntPtr.Zero || !IsWindowVisible(window))continue;
                if(IsIconic(window))ShowWindow(window,9);SetForegroundWindow(window);return true;
            }
            catch(Exception){ }
        }
        return false;
    }
    public void Dispose(){if(owns){mutex.ReleaseMutex();owns=false;}mutex.Dispose();}
}
