using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
namespace StagePad.App;
internal sealed class ProjectBrowser : Border
{
    private sealed record Entry(string FileName,string Path,DateTime Modified)
    { public string Display=>System.IO.Path.GetFileNameWithoutExtension(FileName)+"   ·   "+Modified.ToLocalTime().ToString("g"); }
    private readonly ListBox projects=new(){Background=new SolidColorBrush(Color.FromRgb(15,25,38)),Foreground=Brushes.White,BorderThickness=new Thickness(1),BorderBrush=new SolidColorBrush(Color.FromRgb(41,60,81)),DisplayMemberPath="Display",Margin=new Thickness(0,8,0,8)};
    private readonly TextBox packagePath=new(){MaxLength=1024};
    private readonly ComboBox packages=new(){Margin=new Thickness(0,5,0,5)};
    private readonly StackPanel packagePanel=new(){Visibility=Visibility.Collapsed};
    private readonly TextBox name=new(){MaxLength=110,Margin=new Thickness(0,5,0,8)};
    private readonly TextBlock location=new(){TextTrimming=TextTrimming.CharacterEllipsis,FontSize=11,Foreground=Brushes.LightSlateGray};
    private readonly TextBlock message=new(){TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Orange,Margin=new Thickness(0,6,0,0)};
    private readonly TextBlock editLabel=new(){Foreground=Brushes.White};
    private readonly StackPanel editor=new(){Visibility=Visibility.Collapsed};
    private readonly Button open=new(){Content="Apri progetto"},save=new(){Content="Salva progetto"},rename=new(){Content="Rinomina"},duplicate=new(){Content="Duplica"},delete=new(){Content="Elimina"},confirm=new(){Content="Conferma"};
    private readonly Button export=new(){Content="Esporta show con audio"},import=new(){Content="Importa show"};
    private readonly Func<Request,Task<Reply>> send;
    private readonly Action leave;
    private readonly bool saveMode;
    private bool busy,listing,locked;
    private int refreshSerial;
    private string? editOperation,editSource,overwritePath;
    private string notice="";
    private object[] noticeArguments=[];
    public ProjectBrowser(Func<Request,Task<Reply>> send,Action leave,bool saveMode,string? currentPath)
    {
        this.send=send;this.leave=leave;this.saveMode=saveMode;
        ApplyProjectListStyle();
        Background=new SolidColorBrush(Color.FromRgb(16,27,40));CornerRadius=new CornerRadius(13);Padding=new Thickness(12);
        // The surrounding view may be short at high display scaling. Keep a useful list
        // viewport and let the whole browser scroll instead of clipping its children.
        var scroll=new ScrollViewer {VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled};Child=scroll;
        var root=new StackPanel();scroll.Content=root;
        var header=new DockPanel();root.Children.Add(header);
        var refresh=new Button {Content="Aggiorna elenco",HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(refresh,Dock.Right);header.Children.Add(refresh);
        header.Children.Add(new TextBlock {Text=saveMode?"Salva progetto":"Apri progetto",FontSize=21,Foreground=Brushes.White});root.Children.Add(location);root.Children.Add(projects);
        var actions=new WrapPanel();root.Children.Add(actions);
        foreach(var button in new[]{open,save,rename,duplicate,delete,export,import}){button.Margin=new Thickness(0,0,6,6);actions.Children.Add(button);}
        open.Visibility=saveMode?Visibility.Collapsed:Visibility.Visible;save.Visibility=saveMode?Visibility.Visible:Visibility.Collapsed;
        var back=new Button {Content="Torna a Setup",Margin=new Thickness(0,0,0,6)};actions.Children.Add(back);
        root.Children.Add(editor);editor.Children.Add(editLabel);
        packagePanel.Children.Add(new TextBlock{Text="Pacchetto .stagepad.zip (percorso completo)",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.White});packagePanel.Children.Add(packages);packagePanel.Children.Add(packagePath);editor.Children.Add(packagePanel);
        packages.SelectionChanged+=(_,_)=>{if(packages.SelectedItem is string path)packagePath.Text=path;};editor.Children.Add(name);
        var confirmation=new WrapPanel();confirmation.Children.Add(confirm);confirm.Margin=new Thickness(0,0,8,0);
        var cancel=new Button {Content="Annulla"};confirmation.Children.Add(cancel);editor.Children.Add(confirmation);root.Children.Add(message);
        name.Text=string.IsNullOrWhiteSpace(currentPath)?"Show":Path.GetFileNameWithoutExtension(currentPath);
        L.Preserve(location);L.Preserve(message);L.Preserve(editLabel);
        SizeChanged+=(_,_)=>ResizeList();
        projects.SelectionChanged+=(_,_)=>{if(editOperation==null && saveMode && projects.SelectedItem is Entry e)name.Text=Path.GetFileNameWithoutExtension(e.FileName);UpdateActions();};
        projects.MouseDoubleClick+=async(_,_)=>{if(!saveMode && editOperation==null)await Open();};
        name.TextChanged+=(_,_)=>{overwritePath=null;L.Bind(confirm,ContentControl.ContentProperty,"Conferma");};
        open.Click+=async(_,_)=>await Open();save.Click+=(_,_)=>Begin("save-project");
        rename.Click+=(_,_)=>Begin("rename-project");duplicate.Click+=(_,_)=>Begin("duplicate-project");delete.Click+=(_,_)=>Begin("delete-project");
        export.Click+=(_,_)=>Begin("export-show");import.Click+=(_,_)=>Begin("import-show");
        confirm.Click+=async(_,_)=>await Confirm();cancel.Click+=(_,_)=>EndEdit();
        back.Click+=(_,_)=>{if(!busy)leave();};refresh.Click+=async(_,_)=>{if(!busy){EndEdit();await Refresh();}};
        Loaded+=async(_,_)=>{await Refresh();ResizeList();};
        L.Apply(this);UpdateActions();
    }
    private void ApplyProjectListStyle()
    {
        // Own both templates: the system disabled/inactive states otherwise repaint
        // the list when inline confirmation temporarily disables selection.
        var resources=(ResourceDictionary)XamlReader.Parse("""
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Style TargetType="ListBoxItem">
                <Setter Property="Foreground" Value="#EDF4FC"/>
                <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
                <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
                <Setter Property="Template">
                  <Setter.Value>
                    <ControlTemplate TargetType="ListBoxItem">
                      <Border x:Name="Row" Background="Transparent" BorderBrush="Transparent" BorderThickness="1" CornerRadius="5" Padding="10,8" Margin="3,2">
                        <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="Center"/>
                      </Border>
                      <ControlTemplate.Triggers>
                        <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Row" Property="Background" Value="#1B2D40"/></Trigger>
                        <Trigger Property="IsSelected" Value="True">
                          <Setter TargetName="Row" Property="Background" Value="#173B36"/>
                          <Setter TargetName="Row" Property="BorderBrush" Value="#397F6E"/>
                          <Setter Property="Foreground" Value="#A0F1D9"/>
                        </Trigger>
                      </ControlTemplate.Triggers>
                    </ControlTemplate>
                  </Setter.Value>
                </Setter>
              </Style>
              <Style x:Key="ProjectList" TargetType="ListBox">
                <Setter Property="FocusVisualStyle" Value="{x:Null}"/>
                <Setter Property="Template">
                  <Setter.Value>
                    <ControlTemplate TargetType="ListBox">
                      <Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="7">
                        <ScrollViewer Focusable="False" CanContentScroll="True" VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Auto" Padding="2">
                          <ItemsPresenter/>
                        </ScrollViewer>
                      </Border>
                    </ControlTemplate>
                  </Setter.Value>
                </Setter>
              </Style>
            </ResourceDictionary>
            """);
        projects.Resources=resources;projects.Style=(Style)resources["ProjectList"];
    }
    private void ResizeList()=>projects.Height=Math.Max(130,ActualHeight-(editor.Visibility==Visibility.Visible?290:170));
    public async Task Refresh()
    {
        int stamp=++refreshSerial;string directory=L.ProjectDirectory;location.Text=directory;location.ToolTip=directory;listing=true;UpdateActions();
        try
        {
            var entries=await Task.Run(()=>
            {
                Directory.CreateDirectory(directory);
                return Directory.EnumerateFiles(directory).Where(p=>Path.GetExtension(p).Equals(".json",StringComparison.OrdinalIgnoreCase) &&
                    !Path.GetFileName(p).Equals("last-project.json",StringComparison.OrdinalIgnoreCase)).Take(5000)
                    .Select(p=>new Entry(Path.GetFileName(p),p,File.GetLastWriteTimeUtc(p))).OrderByDescending(p=>p.Modified).ToArray();
            });
            if(stamp!=refreshSerial)return;
            packages.ItemsSource=await Task.Run(()=>Directory.GetFiles(directory,"*.stagepad.zip"));
            projects.ItemsSource=entries;if(entries.Length>0 && !saveMode)projects.SelectedIndex=0;
            Notice(entries.Length==0?"Nessun progetto salvato in questa cartella.":"");
        }
        catch(Exception ex){if(stamp==refreshSerial){projects.ItemsSource=null;Notice(ex.Message);}}
        finally{if(stamp==refreshSerial){listing=false;UpdateActions();}}
    }
    public void Update(StateInfo state){locked=state.ShowLocked;UpdateActions();RefreshNotice();if(editOperation!=null)editLabel.Text=editOperation=="delete-project"?L.F("Eliminare il progetto {0}? I file audio restano sul disco.",Path.GetFileNameWithoutExtension(editSource)??""):L.T("Nome progetto");}
    private void Notice(string key,params object[] arguments){notice=key;noticeArguments=arguments;RefreshNotice();}
    private void RefreshNotice()=>message.Text=noticeArguments.Length==0?L.Message(notice):L.F(notice,noticeArguments);
    private void UpdateActions()
    {
        bool ready=!busy && !listing,selected=projects.SelectedItem is Entry;
        open.IsEnabled=ready && !locked && selected && editOperation==null;save.IsEnabled=ready;
        rename.IsEnabled=duplicate.IsEnabled=delete.IsEnabled=export.IsEnabled=ready && !locked && selected;import.IsEnabled=ready && !locked;
        confirm.IsEnabled=ready && (editOperation=="save-project" || !locked);name.IsEnabled=ready;projects.IsEnabled=ready && editOperation==null;
    }
    private void Begin(string operation)
    {
        if(busy || listing || (locked && operation!="save-project"))return;
        if(operation is not ("save-project" or "import-show") && projects.SelectedItem is not Entry)return;
        editOperation=operation;editSource=(projects.SelectedItem as Entry)?.Path;overwritePath=null;
        if(operation!="save-project")name.Text=Path.GetFileNameWithoutExtension(editSource)+(operation=="duplicate-project"?" - "+L.T("Copia"):"");
        packagePanel.Visibility=operation=="import-show"?Visibility.Visible:Visibility.Collapsed;
        if(operation=="import-show")name.Text="Show-importato";
        name.Visibility=operation=="delete-project"?Visibility.Collapsed:Visibility.Visible;
        editLabel.Text=operation=="delete-project"?L.F("Eliminare il progetto {0}? I file audio restano sul disco.",Path.GetFileNameWithoutExtension(editSource)??""):L.T("Nome progetto");
        L.Bind(confirm,ContentControl.ContentProperty,operation=="delete-project"?"Elimina":"Conferma");editor.Visibility=Visibility.Visible;Notice("");UpdateActions();ResizeList();
        editor.BringIntoView();if(operation!="delete-project"){name.Focus();name.SelectAll();}
    }
    private void EndEdit(){packagePanel.Visibility=Visibility.Collapsed;editOperation=null;editSource=null;overwritePath=null;editor.Visibility=Visibility.Collapsed;Notice("");UpdateActions();ResizeList();}
    private async Task Open()
    {
        if(!open.IsEnabled || projects.SelectedItem is not Entry entry)return;
        busy=true;UpdateActions();Notice("Apertura progetto…");
        try{var reply=await send(new("load-project",Path:entry.Path));if(reply.Ok)leave();else Notice(reply.Error??"Comando non riuscito.");}
        catch(Exception ex){Notice(ex.Message);}finally{busy=false;UpdateActions();}
    }
    private async Task Confirm()
    {
        if(busy || listing || editOperation==null || (locked && editOperation!="save-project"))return;
        try
        {
            string operation=editOperation;
            if(operation is "export-show" or "import-show")
            {
                string target=Path.Combine(L.ProjectDirectory,ProjectDirectoryPolicy.FileName(name.Text));
                string source=operation=="import-show"?packagePath.Text.Trim():Path.ChangeExtension(target,"stagepad.zip");
                if(!Path.IsPathFullyQualified(source))throw new InvalidDataException("Inserisci il percorso completo del pacchetto.");
                busy=true;UpdateActions();Notice("Operazione sul pacchetto in corso…");
                var result=await send(new(operation,Path:source,TargetPath:operation=="export-show"?editSource:target));
                if(result.Ok){EndEdit();await Refresh();Notice("Operazione completata.");}else Notice(result.Error??"Comando non riuscito.");return;
            }
            string? destination=operation=="delete-project"?null:Path.Combine(operation=="save-project"?L.ProjectDirectory:Path.GetDirectoryName(editSource!)!,ProjectDirectoryPolicy.FileName(name.Text));
            if(operation=="save-project" && File.Exists(destination) && !string.Equals(destination,overwritePath,StringComparison.OrdinalIgnoreCase))
            {overwritePath=destination;Notice("Il progetto {0} esiste già. Vuoi sovrascriverlo?",Path.GetFileName(destination));L.Bind(confirm,ContentControl.ContentProperty,"Sovrascrivi");return;}
            busy=true;UpdateActions();
            var reply=await send(new(operation,Path:operation=="save-project"?destination:editSource,TargetPath:operation=="save-project"?null:destination));
            if(reply.Ok){EndEdit();await Refresh();Notice("Operazione completata.");}else Notice(reply.Error??"Comando non riuscito.");
        }
        catch(Exception ex){Notice(ex.Message);}finally{busy=false;UpdateActions();}
    }
}
