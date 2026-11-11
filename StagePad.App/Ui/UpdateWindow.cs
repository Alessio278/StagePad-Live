using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace StagePad.App;
internal sealed class UpdateWindow : Window
{
    private readonly AppUpdates updates=new();
    private readonly CancellationTokenSource cancel=new();
    private readonly TextBlock status=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.LightSlateGray,Margin=new Thickness(0,14,0,14)};
    private readonly Button check=new(){Content="Controlla aggiornamenti"},install=new(){Content="Scarica e aggiorna",IsEnabled=false},rollback=new(){Content="Ripristina versione precedente"};
    private UpdateRelease? release;
    private bool busy,handedOff;
    public UpdateWindow(Func<Request,Task<Reply>> send,int supervisor)
    {
        WindowAppearance.Apply(this);Title="Aggiornamenti";Width=540;Height=420;MinWidth=440;MinHeight=340;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        Background=new SolidColorBrush(Color.FromRgb(11,17,26));Foreground=Brushes.White;
        var panel=new StackPanel{Margin=new Thickness(24)};Content=panel;
        panel.Children.Add(new TextBlock{Text="StagePad "+AppUpdates.Current.ToString(3),FontSize=23});
        panel.Children.Add(new TextBlock{Text="Aggiornamenti solo a riproduzione ferma. L’app verrà riavviata; progetti e impostazioni restano conservati.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,8)});
        foreach(var button in new[]{check,install,rollback}){button.Margin=new Thickness(0,4,0,4);panel.Children.Add(button);}
        panel.Children.Add(status);L.Preserve(status);
        string target=Environment.ProcessPath!;rollback.IsEnabled=updates.Previous(target)!=null;
        void Buttons(){check.IsEnabled=!busy;install.IsEnabled=!busy && release!=null;rollback.IsEnabled=!busy && updates.Previous(target)!=null;}
        check.Click+=async(_,_)=>
        {
            if(busy)return;busy=true;Buttons();status.Text=L.T("Verifica aggiornamenti…");
            try{release=await updates.Check(ct:cancel.Token);status.Text=release==null?L.T("Nessuna nuova release stabile disponibile."):L.F("Disponibile StagePad {0}.",release.Version.ToString());}
            catch(OperationCanceledException){}catch(Exception ex){status.Text=L.Message(ex.Message);}finally{busy=false;Buttons();}
        };
        async Task Apply(bool previous)
        {
            if(busy)return;busy=true;Buttons();bool maintenance=false;
            try
            {
                var guard=await send(new("prepare-update"));if(!guard.Ok)throw new InvalidOperationException(guard.Error);
                maintenance=true;status.Text=L.T("Preparazione aggiornamento…");
                var candidate=previous?updates.StagePrevious(target):await updates.Download(release??throw new InvalidOperationException("Nessuna release selezionata."),cancel.Token);
                cancel.Token.ThrowIfCancellationRequested();UpdateExecutor.Launch(candidate,supervisor,previous);handedOff=true;LifecycleSignals.RequestClose("-"+supervisor);
            }
            catch(OperationCanceledException){}catch(Exception ex){status.Text=L.Message(ex.Message);}
            finally
            {
                if(maintenance && !handedOff)try{await send(new("cancel-update"));}catch{}
                busy=false;if(!handedOff)Buttons();
            }
        }
        install.Click+=async(_,_)=>await Apply(false);rollback.Click+=async(_,_)=>await Apply(true);
        Closed+=(_,_)=>{if(!handedOff)cancel.Cancel();};L.Apply(this);
    }
}
