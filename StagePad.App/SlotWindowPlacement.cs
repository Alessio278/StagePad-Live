namespace StagePad.App;
internal readonly record struct ScreenBox(double Left,double Top,double Width,double Height)
{
    public double Right=>Left+Width;
    public double Bottom=>Top+Height;
}
internal static class SlotWindowPlacement
{
    public static ScreenBox Beside(ScreenBox work,ScreenBox slot,double width=650,double height=950,double gap=12)
    {
        width=Math.Min(width,Math.Max(1,work.Width));height=Math.Min(height,Math.Max(1,work.Height));
        double rightSpace=work.Right-slot.Right-gap,leftSpace=slot.Left-work.Left-gap;
        double x=rightSpace>=width?slot.Right+gap:leftSpace>=width?slot.Left-gap-width:
            rightSpace>=leftSpace?slot.Right+gap:slot.Left-gap-width;
        x=Math.Clamp(x,work.Left,work.Right-width);
        double y=Math.Clamp(slot.Top,work.Top,work.Bottom-height);
        return new(x,y,width,height);
    }
}
