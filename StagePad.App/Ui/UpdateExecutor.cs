using System.Diagnostics;
using System.IO;
using System.Text.Json;
namespace StagePad.App;
internal static class UpdateExecutor
{
    public static void Launch(UpdateCandidate candidate,int supervisor,bool rollback)
    {
        string current=Environment.ProcessPath??throw new InvalidOperationException("Eseguibile non disponibile.");
        if(!current.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Avvia l’EXE pubblicato per aggiornare.");
        string root=Path.Combine(Log.Root,"updates");Directory.CreateDirectory(root);
        string runner=Path.Combine(root,"update-runner-"+Guid.NewGuid().ToString("N")+".exe");File.Copy(current,runner);
        using var parent=Process.GetProcessById(supervisor);
        var start=new ProcessStartInfo(runner){UseShellExecute=false,WorkingDirectory=root};
        foreach(string argument in new[]{"--apply-update",current,candidate.Path,candidate.Sha256,rollback?"1":"0",supervisor.ToString(),parent.StartTime.ToUniversalTime().Ticks.ToString(),candidate.Version.ToString()})start.ArgumentList.Add(argument);
        using var runnerProcess=Process.Start(start)??throw new IOException("Avvio aggiornamento non riuscito.");
    }
    public static int Run(string[] args)
    {
        string target=args[1],candidate=args[2],hash=args[3],root=Path.Combine(Log.Root,"updates");bool rollback=args[4]=="1";
        string? localBackup=null;Process? launched=null;bool success=false;
        try
        {
            candidate=Path.GetFullPath(candidate);
            if(!candidate.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Percorso aggiornamento non valido.");
            try{using var parent=Process.GetProcessById(int.Parse(args[5]));if(parent.StartTime.ToUniversalTime().Ticks==long.Parse(args[6]) && !parent.WaitForExit(30000))throw new TimeoutException("StagePad non ha completato la chiusura.");}catch(ArgumentException){}
            using(var mutex=new Mutex(false,"Local\\StagePad-Phase1-"+Environment.UserName))
            {
                bool owns;try{owns=mutex.WaitOne(2000);}catch(AbandonedMutexException){owns=true;}
                if(!owns)throw new IOException("Un’altra istanza di StagePad è ancora attiva.");
                try{AppUpdates.ValidateBinary(candidate,Version.Parse(args[7]));localBackup=UpdateFiles.Replace(target,candidate,hash);}finally{mutex.ReleaseMutex();}
            }
            string token=Guid.NewGuid().ToString("N"),health=HealthPath(token);
            var start=new ProcessStartInfo(target){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(target)!};
            if(!rollback){start.ArgumentList.Add("--post-update");start.ArgumentList.Add(token);}
            launched=Process.Start(start)??throw new IOException("Nuova versione non avviata.");
            if(!rollback)
            {
                long deadline=Environment.TickCount64+45000;
                while(!File.Exists(health) && !launched.HasExited && Environment.TickCount64<deadline)Thread.Sleep(100);
                if(!File.Exists(health))throw new IOException("La nuova versione non ha superato il controllo di avvio.");
                File.Delete(health);
            }
            string previous=Path.Combine(root,"previous.exe");File.Copy(localBackup,previous,true);
            var info=FileVersionInfo.GetVersionInfo(previous);string version=(info.ProductVersion??"0.0.0").Split('+')[0];
            File.WriteAllText(Path.Combine(root,"previous.json"),JsonSerializer.Serialize(new PreviousApp(target,version,UpdateFiles.Hash(previous))));
            success=true;return 0;
        }
        catch(Exception ex)
        {
            Log.Error("update-install",ex);
            try
            {
                if(launched!=null && !launched.HasExited){LifecycleSignals.RequestClose("-"+launched.Id);if(!launched.WaitForExit(7000))throw new IOException("Chiudi la nuova istanza prima di ripristinare.");}
                if(localBackup!=null)
                {
                    using var recoveryMutex=new Mutex(false,"Local\\StagePad-Phase1-"+Environment.UserName);
                    bool acquired;try{acquired=recoveryMutex.WaitOne(5000);}catch(AbandonedMutexException){acquired=true;}
                    if(!acquired)throw new IOException("Un’altra istanza è attiva: ripristino rinviato.");
                    try{UpdateFiles.Restore(target,localBackup);}finally{recoveryMutex.ReleaseMutex();}
                }
                if(File.Exists(target))Process.Start(new ProcessStartInfo(target){UseShellExecute=false})?.Dispose();
            }
            catch(Exception restore){Log.Error("update-restore",restore);}
            System.Windows.MessageBox.Show("Aggiornamento non completato. La copia precedente è conservata.\n"+ex.Message,"StagePad");return 2;
        }
        finally{launched?.Dispose();if(success && localBackup!=null)try{File.Delete(localBackup);}catch{}}
    }
    public static string HealthPath(string token)
    {if(!Guid.TryParseExact(token,"N",out _))throw new InvalidDataException("Token aggiornamento non valido.");return Path.Combine(Log.Root,"updates","health-"+token+".json");}
    public static void Healthy(string token){string path=HealthPath(token);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllText(path,"ready");}
}
