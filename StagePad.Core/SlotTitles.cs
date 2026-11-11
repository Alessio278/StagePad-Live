namespace StagePad.Core;
public static class SlotTitles
{
    private static string Track(string? file)=>string.IsNullOrWhiteSpace(file)?"":Path.GetFileNameWithoutExtension(file.Replace('\\','/'));
    public static bool Automatic(SlotSettings slot,int index)=>slot.AutoName??
        (string.IsNullOrWhiteSpace(slot.Name) || slot.Name=="Slot" || slot.Name==$"Slot {index+1}" ||
         (!string.IsNullOrWhiteSpace(slot.File) && slot.Name==Track(slot.File)));
    public static string Display(SlotSettings slot,int index)
    {
        if(!Automatic(slot,index))return slot.Name;
        string track=Track(slot.File);return track.Length>0?track:$"Slot {index+1}";
    }
    public static SlotSettings Normalize(SlotSettings slot,int index)
    {
        bool auto=string.IsNullOrWhiteSpace(slot.Name) || Automatic(slot,index);
        return slot with{AutoName=auto,Name=auto?$"Slot {index+1}":slot.Name.Trim()};
    }
}
public enum MidiControlMode { Absolute, RelativeTwosComplement, RelativeBinaryOffset, RelativeSignMagnitude }
public sealed record MidiCcBinding(int Channel=1,int Controller=-1,MidiControlMode Mode=MidiControlMode.Absolute,bool Pickup=false,float Step=.01f)
{
    public bool Matches(int channel,int controller)=>Controller>=0 && Channel==channel && Controller==controller;
    public static void Validate(MidiCcBinding? master,MidiCcBinding? track)
    {
        if(master is null || track is null || master.Channel is <1 or >16 || track.Channel is <1 or >16 || master.Controller is <-1 or >127 || track.Controller is <-1 or >127 || !Enum.IsDefined(master.Mode) || !Enum.IsDefined(track.Mode) || !float.IsFinite(master.Step) || !float.IsFinite(track.Step) || master.Step is <.001f or >.1f || track.Step is <.001f or >.1f)
            throw new InvalidDataException("Assegnazione volume MIDI non valida: canale 1-16 e CC da -1 a 127.");
        if(master.Controller>=0 && track.Matches(master.Channel,master.Controller))throw new InvalidDataException("Master e traccia devono usare controlli MIDI diversi.");
    }
}
