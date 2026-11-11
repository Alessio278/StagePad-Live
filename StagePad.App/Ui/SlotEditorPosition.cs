using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace StagePad.App;
internal static class SlotEditorPosition
{
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size;public NativeRect Monitor,Work;public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromRect(ref NativeRect rect,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Auto)] private static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll",SetLastError=true)] private static extern bool SetWindowPos(IntPtr window,IntPtr after,int x,int y,int width,int height,uint flags);
    public static void Attach(Window editor,FrameworkElement slot)
    {
        editor.WindowStartupLocation=WindowStartupLocation.Manual;
        void Place()
        {
            try
            {
                var topLeft=slot.PointToScreen(new Point());var bottomRight=slot.PointToScreen(new Point(slot.ActualWidth,slot.ActualHeight));
                var rect=new NativeRect { Left=(int)Math.Floor(topLeft.X),Top=(int)Math.Floor(topLeft.Y),Right=(int)Math.Ceiling(bottomRight.X),Bottom=(int)Math.Ceiling(bottomRight.Y) };
                var info=new MonitorInfo { Size=Marshal.SizeOf<MonitorInfo>() };
                if(!GetMonitorInfo(MonitorFromRect(ref rect,2),ref info))throw new Win32Exception();
                var area=new ScreenBox(info.Work.Left,info.Work.Top,info.Work.Right-info.Work.Left,info.Work.Bottom-info.Work.Top);
                var card=new ScreenBox(rect.Left,rect.Top,rect.Right-rect.Left,rect.Bottom-rect.Top);
                var placement=SlotWindowPlacement.Beside(area,card);
                var handle=new WindowInteropHelper(editor).Handle;
                void Position()
                {
                    if(!SetWindowPos(handle,IntPtr.Zero,(int)placement.Left,(int)placement.Top,(int)placement.Width,(int)placement.Height,0x0004|0x0010|0x0200))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                // Move first so the DPI query belongs to the target monitor, then
                // keep WPF dimensions consistent with the requested physical pixels.
                Position();double scale=Math.Max(96,GetDpiForWindow(handle))/96d;
                editor.MinWidth=Math.Min(460,placement.Width/scale);editor.MinHeight=Math.Min(450,placement.Height/scale);
                editor.Width=placement.Width/scale;editor.Height=placement.Height/scale;Position();
            }
            catch(Exception ex){Log.Warn("slot-window-position",ex.Message);}
        }
        editor.SourceInitialized+=(_,_)=>Place();editor.Loaded+=(_,_)=>Place();
    }
}
