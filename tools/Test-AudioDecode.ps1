param([string]$Exe, [string]$TempRoot)
$ErrorActionPreference = 'Stop'
[IO.Directory]::CreateDirectory($TempRoot) | Out-Null
function Decode([string]$source, [string]$name) {
    $pcm = Join-Path $TempRoot "$name.pcm"
    $process = Start-Process -FilePath $Exe -ArgumentList "--decode `"$source`" `"$pcm`" $PID" -PassThru
    if (!$process.WaitForExit(30000)) { $process.Kill(); throw 'Decoder timed out' }
    if ($process.ExitCode -ne 0) { throw "Decoder failed for $name" }
    if (!(Test-Path "$pcm.wave")) { throw 'Waveform missing' }
    return $pcm
}
$mp3 = Decode (Join-Path $PSScriptRoot 'audio-fixtures/tone.mp3') 'mp3'
$bytes = [IO.File]::ReadAllBytes($mp3)
if ($bytes.Length % 8 -ne 0 -or $bytes.Length -lt 375000 -or $bytes.Length -gt 408000) { throw 'Unexpected resampled frame count' }
$peak = 0.0
for ($i = 0; $i -lt $bytes.Length; $i += 8) {
    $left = [BitConverter]::ToSingle($bytes,$i)
    $right = [BitConverter]::ToSingle($bytes,$i+4)
    if ([float]::IsNaN($left) -or $left -ne $right) { throw 'Invalid mono to stereo conversion' }
    $peak = [Math]::Max($peak,[Math]::Abs($left))
}
if ($peak -lt 0.10 -or $peak -gt 0.14) { throw "Unexpected decoded reference peak $peak" }
# IEEE float stereo WAV with a peak of 2.0. Lower samples must scale proportionally.
$wav = Join-Path $TempRoot 'overs.wav'
$writer = [IO.BinaryWriter]::new([IO.File]::Create($wav))
try {
    $ascii = [Text.Encoding]::ASCII
    $writer.Write($ascii.GetBytes('RIFF')); $writer.Write([int]52)
    $writer.Write($ascii.GetBytes('WAVEfmt ')); $writer.Write([int]16)
    $writer.Write([int16]3); $writer.Write([int16]2); $writer.Write([int]48000)
    $writer.Write([int]384000); $writer.Write([int16]8); $writer.Write([int16]32)
    $writer.Write($ascii.GetBytes('data')); $writer.Write([int]16)
    foreach ($sample in @(2,-2,0.5,-0.5)) { $writer.Write([single]$sample) }
} finally { $writer.Dispose() }
$scaled = [IO.File]::ReadAllBytes((Decode $wav 'overs'))
$expected = @(1,-1,0.25,-0.25)
if ($scaled.Length -ne 16) { throw 'Partial PCM' }
for ($i=0; $i -lt 4; $i++) {
    if ([BitConverter]::ToSingle($scaled,4*$i) -ne $expected[$i]) { throw 'Incorrect whole-track peak correction' }
}
