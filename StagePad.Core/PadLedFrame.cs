namespace StagePad.Core;
// Generic Note On feedback only. Values are device-specific palette/level codes,
// not a universal RGB protocol. Disabled until a dedicated output is selected.
public sealed record PadLedFrame(string? OutputId,int[] Messages,long Tick=0)
{
    public void Validate()
    {
        if((OutputId?.Length??0)>512 || Messages==null || Messages.Length>Projects.MaxSlots)throw new InvalidDataException("Profilo LED non valido.");
        foreach(int raw in Messages)if((raw&0xF0)!=0x90 || (raw&unchecked((int)0xFF808000))!=0)throw new InvalidDataException("Messaggio LED non valido.");
    }
    public static int Message(SlotSettings slot,bool active)=>MidiCodec.Note(slot.PadChannel,slot.PadNote,SlotContent.Configured(slot)?(active?slot.LedActiveValue:slot.LedIdleValue):0,true);
}
