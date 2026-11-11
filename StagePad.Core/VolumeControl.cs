namespace StagePad.Core;
public sealed class VolumeControl
{
    private MidiCcBinding? binding;
    private int target=int.MinValue;
    private float? previous,lastOutput;
    private bool latched;
    public void Reset(){binding=null;previous=null;lastOutput=null;latched=false;target=int.MinValue;}
    public float? Apply(MidiCcBinding map,int raw,float current,int targetId)
    {
        if(raw is <0 or >127)throw new ArgumentOutOfRangeException(nameof(raw));
        if(map.Mode!=MidiControlMode.Absolute)
        {
            int delta=map.Mode switch{MidiControlMode.RelativeTwosComplement=>raw<64?raw:raw-128,MidiControlMode.RelativeBinaryOffset=>raw-64,_=>(raw&63)*((raw&64)!=0?-1:1)};
            return Math.Clamp(current+delta*map.Step,0,1);
        }
        float value=raw/127f;
        if(!map.Pickup)return value;
        if(binding!=map || target!=targetId || (lastOutput.HasValue && Math.Abs(lastOutput.Value-current)>.5f/127))
        {binding=map;target=targetId;latched=false;previous=null;lastOutput=null;}
        if(!latched)
        {
            latched=Math.Abs(value-current)<=1.5f/127 || (previous.HasValue && (previous.Value-current)*(value-current)<=0);
            previous=value;if(!latched)return null;
        }
        lastOutput=value;return value;
    }
}
