param([int]$Seconds = 65, [int]$Rate = 44100)
$ErrorActionPreference = 'Stop'
if ($Seconds -lt 1 -or $Seconds -gt 1500) { throw 'Usa durata tra 1 e 1500 secondi.' }
if ($Rate -notin 44100,48000) { throw 'Rate ammessi: 44100, 48000.' }
$folder = Join-Path $PSScriptRoot 'audio-test'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$path = Join-Path $folder "tono-${Seconds}s-${Rate}Hz.wav"
$writer = [System.IO.BinaryWriter]::new([System.IO.File]::Create($path))
try {
    $length = $Rate * $Seconds * 4
    $writer.Write([System.Text.Encoding]::ASCII.GetBytes('RIFF'))
    $writer.Write([int](36+$length))
    $writer.Write([System.Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
    $writer.Write([int]16); $writer.Write([int16]1); $writer.Write([int16]2)
    $writer.Write([int]$Rate); $writer.Write([int]($Rate*4)); $writer.Write([int16]4); $writer.Write([int16]16)
    $writer.Write([System.Text.Encoding]::ASCII.GetBytes('data')); $writer.Write([int]$length)
    # -30 dBFS: iniziare comunque a volume basso sull'impianto.
    for ($i=0; $i -lt ($Rate*$Seconds); $i++) {
        $envelope = [Math]::Min(1.0, [Math]::Min($i/($Rate*0.01), (($Rate*$Seconds)-1-$i)/($Rate*0.01)))
        $sample = [int16](1000*$envelope*[Math]::Sin(2*[Math]::PI*440*$i/$Rate))
        $writer.Write($sample); $writer.Write($sample)
    }
} finally { $writer.Dispose() }
[System.IO.File]::WriteAllText((Join-Path $folder 'corrotto.wav'), 'questo non e un WAV')
Write-Host "File di prova: $folder"
