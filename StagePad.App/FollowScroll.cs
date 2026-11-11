namespace StagePad.App;
internal static class FollowScroll
{
    // Move only far enough to reveal the target. Coordinates are WPF logical units.
    public static double Offset(double current,double viewport,double extent,double start,double size)
    {
        if(!double.IsFinite(current) || !double.IsFinite(viewport) || !double.IsFinite(extent) ||
            !double.IsFinite(start) || !double.IsFinite(size) || viewport<=0 || size<=0)return current;
        double maximum=Math.Max(0,extent-viewport);current=Math.Clamp(current,0,maximum);
        double end=start+size;
        if(size>viewport)
        {
            // If the viewport is already inside an oversized cell, keep it still.
            if(start<=current && end>=current+viewport)return current;
            return Math.Clamp(start,0,maximum);
        }
        double requested=start<current?start:end>current+viewport?end-viewport:current;
        return Math.Clamp(requested,0,maximum);
    }
}
