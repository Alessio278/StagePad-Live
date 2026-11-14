namespace StagePad.App;

internal readonly record struct WaveformIdentity(string Instance,int Slot,int Revision)
{
    public static bool Accepts(WaveformIdentity expected,bool ok,string? instance,int? slot,int? revision,bool ready,float[]? peaks)
        =>ok && ready && instance==expected.Instance && slot==expected.Slot && revision==expected.Revision && peaks is {Length:>0 and <=16384} && peaks.All(p=>float.IsFinite(p) && p is >=0 and <=1);
}
