using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
namespace StagePad.App;
internal sealed class UiLanguage : INotifyPropertyChanged
{
    public string Code { get; set; }="it";
    public int Revision { get; private set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public void Refresh(){Revision++;PropertyChanged?.Invoke(this,new(nameof(Revision)));}
}
internal static class L
{
    public static UiLanguage Current { get; }=new();
    public static string Language=>Current.Code;
    public static event Action? Changed;
    public static readonly DependencyProperty PreserveTextProperty=DependencyProperty.RegisterAttached("PreserveText",typeof(bool),typeof(L),new PropertyMetadata(false));
    private static UiPreferences preferences=new();
    public static bool FollowTriggeredSlot=>preferences.FollowTriggeredSlot;
    public static string ProjectDirectory=>preferences.ProjectDirectory??ProjectFiles.DefaultDirectory;
    public static event Action? DirectoryChanged;
    private static string SettingsPath=>Path.Combine(Log.Root,"settings.json");
    public static void Initialize()
    {
        try
        {
            if(File.Exists(SettingsPath))
            {
                preferences=UiPreferences.Parse(File.ReadAllText(SettingsPath));
                Current.Code=preferences.Language;
            }
        }
        catch(Exception ex){Log.Warn("settings-read",ex.Message);}
    }
    public static bool Change(string language)
    {
        if(!LocalizationCatalog.Languages.Contains(language))throw new ArgumentOutOfRangeException(nameof(language));
        if(Language==language)return true;
        var next=preferences with { Language=language };
        bool saved=Save(next);preferences=next;
        Current.Code=language;Current.Refresh();
        if(Application.Current!=null)foreach(Window window in Application.Current.Windows)Apply(window);
        Changed?.Invoke();return saved;
    }
    public static bool SetFollowTriggeredSlot(bool enabled)
    {
        var next=preferences with { FollowTriggeredSlot=enabled };
        bool saved=Save(next);preferences=next;return saved;
    }
    public static bool SetProjectDirectory(string? directory)
    {
        var next=preferences with { ProjectDirectory=directory };
        bool saved=Save(next);preferences=next;DirectoryChanged?.Invoke();return saved;
    }
    private static bool Save(UiPreferences value)
    {
        try
        {
            Directory.CreateDirectory(Log.Root);string temporary=SettingsPath+".tmp";
            File.WriteAllText(temporary,value.ToJson());File.Move(temporary,SettingsPath,true);return true;
        }
        catch(Exception ex){Log.Warn("settings-save",ex.Message);return false;}
    }
    public static string T(string key)=>LocalizationCatalog.Translate(Language,key);
    public static string Message(string text)=>LocalizationCatalog.Message(Language,text);
    public static string F(string key,params object[] args)=>string.Format(CultureInfo.GetCultureInfo(Language),T(key),args);
    private sealed class KeyConverter(string key,object[] args) : IValueConverter
    {
        public object Convert(object value,Type type,object parameter,CultureInfo culture)=>args.Length==0?T(key):F(key,args);
        public object ConvertBack(object value,Type type,object parameter,CultureInfo culture)=>Binding.DoNothing;
    }
    public static void Bind(DependencyObject target,DependencyProperty property,string key,params object[] args)
    {
        if(!LocalizationCatalog.Has(key))return;
        BindingOperations.SetBinding(target,property,new Binding(nameof(UiLanguage.Revision)){Source=Current,Converter=new KeyConverter(key,args),Mode=BindingMode.OneWay});
    }
    public static void Preserve(DependencyObject target)=>target.SetValue(PreserveTextProperty,true);
    public static void Apply(DependencyObject root)
    {
        var visited=new HashSet<DependencyObject>();var pending=new Stack<DependencyObject>();pending.Push(root);
        while(pending.TryPop(out var item))
        {
            if(!visited.Add(item))continue;
            void Property(DependencyProperty property)
            {
                if(BindingOperations.IsDataBound(item,property))return;
                if(item.GetValue(property) is string value)Bind(item,property,value);
            }
            if(item is TextBlock && !(bool)item.GetValue(PreserveTextProperty))Property(TextBlock.TextProperty);
            if(item is ContentControl)Property(ContentControl.ContentProperty);
            if(item is HeaderedContentControl)Property(HeaderedContentControl.HeaderProperty);
            if(item is FrameworkElement){if(!(bool)item.GetValue(PreserveTextProperty))Property(FrameworkElement.ToolTipProperty);Property(System.Windows.Automation.AutomationProperties.NameProperty);}
            if(item is Window)Property(Window.TitleProperty);
            if(item is ComboBox combo){object selected=combo.SelectedValue;combo.Items.Refresh();if(selected!=null)combo.SelectedValue=selected;}
            if(item is Popup popup && popup.Child!=null)pending.Push(popup.Child);
            foreach(object child in LogicalTreeHelper.GetChildren(item))if(child is DependencyObject dependency)pending.Push(dependency);
        }
    }
}
