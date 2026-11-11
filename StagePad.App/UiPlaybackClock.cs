using System.Globalization;

namespace StagePad.App;
internal readonly record struct PlayerReading(double Position, long Elapsed, long Remaining, long Duration);

/// <summary>Presentation-only clock. Uses the engine's boot-time timestamp, never IPC receipt time.</summary>
internal static class UiPlaybackClock
{
    public static PlayerReading Read(double position, double duration, bool playing, bool ended, long engineTick, long nowTick)
    {
        duration = double.IsFinite(duration) ? Math.Max(0, duration) : 0;
        position = double.IsFinite(position) ? Math.Clamp(position, 0, duration) : 0;
        // TickCount64 has the same origin in each Windows process. Predict only a short
        // gap; a missing engine response must never let the UI clock run indefinitely.
        if (playing && engineTick > 0 && nowTick >= engineTick)
            position = Math.Min(duration, position + Math.Min(nowTick - engineTick, 500) / 1000.0);
        if (ended) position = duration;
        // Quantize once. Remaining is the complement of the displayed elapsed value,
        // rather than independently truncating duration-position at a different phase.
        long total = (long)Math.Ceiling(duration - 0.0000001);
        total = Math.Max(0, total);
        bool finished = ended || (duration > 0 && position >= duration);
        long elapsed = finished ? total : Math.Clamp((long)Math.Floor(position + 0.0000001), 0, total);
        return new(position, elapsed, total - elapsed, total);
    }
    public static string Format(long seconds)
    {
        seconds = Math.Max(0, seconds);
        return seconds >= 3600
            ? string.Create(CultureInfo.InvariantCulture, $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}")
            : string.Create(CultureInfo.InvariantCulture, $"{seconds / 60:00}:{seconds % 60:00}");
    }
}
