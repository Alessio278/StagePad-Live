namespace StagePad.App;
internal sealed record PaletteColor(string Name,string Hex) { public override string ToString()=>Name; }
internal static class SlotPalette
{
    public static IReadOnlyList<PaletteColor> Colors { get; } = Array.AsReadOnly(new PaletteColor[]
    {
        new("Blu","#4A78BC"),new("Turchese","#1DAA90"),new("Ciano","#2AA9C7"),new("Rosso","#E65B67"),new("Arancione","#F1984D"),
        new("Giallo","#DAC24C"),new("Viola","#9B78DB"),new("Rosa","#D36D9F"),new("Verde","#78B55A"),new("Sabbia","#A8865F"),
        new("Ardesia","#7F93AA"),new("Indaco","#5C6CD8"),new("Menta","#60C6AB"),new("Lavanda","#B49AE2"),new("Corallo","#EF8B80"),
        new("Prugna","#9667A8"),new("Bosco","#429A72"),new("Ocra","#C89C46"),new("Grigio","#98A4B5"),new("Ghiaccio","#84BED3")
    });
}
