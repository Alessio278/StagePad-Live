namespace StagePad.App;
internal static class HorizontalWheelMotion
{
    // Preserve fractional high-resolution trackpad deltas instead of truncating
    // to whole wheel notches. Positive WM_MOUSEHWHEEL deltas move right.
    public static double Pixels(int delta)=>delta*(48.0/120.0);
}
