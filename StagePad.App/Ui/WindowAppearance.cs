using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace StagePad.App;
internal static class WindowAppearance
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    public static void Apply(Window window)
    {
        window.Icon=new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/StagePad;component/Assets/StagePad.png",UriKind.Absolute));
        window.Loaded+=(_,_)=>L.Apply(window);
        window.UseLayoutRounding = true; window.SnapsToDevicePixels = true;
        window.SourceInitialized += (_, _) =>
        {
            try
            {
                IntPtr handle = new WindowInteropHelper(window).Handle;
                int dark = 1;
                if (DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int)) != 0)
                    DwmSetWindowAttribute(handle, 19, ref dark, sizeof(int));
            }
            catch (Exception ex) { Log.Warn("ui-titlebar", ex.Message); }
        };
    }
}
