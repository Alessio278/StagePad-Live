using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace StagePad.App;
internal static class StartupWindowPosition
{
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size;public NativeRect Monitor,Work;public uint Flags; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(NativePoint point,uint flags);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll",SetLastError=true)] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    public static void Attach(Window window)
    {
        IntPtr targetMonitor=IntPtr.Zero;
        void Place()
        {
            try
            {
                var handle=new WindowInteropHelper(window).Handle;
                if(targetMonitor==IntPtr.Zero)targetMonitor=GetCursorPos(out var pointer)?MonitorFromPoint(pointer,2):MonitorFromWindow(handle,2);
                var info=new MonitorInfo { Size=Marshal.SizeOf<MonitorInfo>() };
                if(!GetMonitorInfo(targetMonitor,ref info))throw new Win32Exception();
                var area=new ScreenBox(info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top);
                var placement=StartupWindowPlacement.Center(area);
                void Position()
                {
                    if(!SetWindowPos(handle,IntPtr.Zero,(int)Math.Round(placement.Left),(int)Math.Round(placement.Top),(int)placement.Width,(int)placement.Height,0x0004|0x0010|0x0200))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                Position();double scale=Math.Max(96,GetDpiForWindow(handle))/96d;
                window.MinWidth=Math.Min(850,placement.Width/scale);window.MinHeight=Math.Min(650,placement.Height/scale);
                window.Width=placement.Width/scale;window.Height=placement.Height/scale;Position();
            }
            catch(Exception ex){Log.Warn("main-window-position",ex.Message);}
        }
        window.SourceInitialized+=(_,_)=>Place();window.Loaded+=(_,_)=>Place();
    }
}
