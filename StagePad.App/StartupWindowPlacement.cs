namespace StagePad.App;
internal static class StartupWindowPlacement
{
    public static ScreenBox Center(ScreenBox work,double width=1250,double height=960)
    {
        width=Math.Min(width,Math.Max(1,work.Width));height=Math.Min(height,Math.Max(1,work.Height));
        return new(work.Left+(work.Width-width)/2,work.Top+(work.Height-height)/2,width,height);
    }
}
