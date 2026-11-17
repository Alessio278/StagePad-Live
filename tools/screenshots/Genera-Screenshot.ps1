param([string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $PSScriptRoot ('screenshots-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) { throw 'La cartella di destinazione esiste gia. Scegli una cartella nuova.' }
$exe = Join-Path $PSScriptRoot 'StagePad-Screenshots.exe'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Esegui lo script dalla cartella estratta completa, con StagePad-Screenshots.exe.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$utf8 = New-Object System.Text.UTF8Encoding($false)
$languages = @('it', 'en', 'es', 'fr')
foreach ($language in $languages) {
    $folder = Join-Path $OutputDirectory $language
    New-Item -ItemType Directory -Path $folder | Out-Null
    Write-Host ('Screenshot ' + $language + ': acquisizione in corso...')
    $process = Start-Process -FilePath $exe -ArgumentList @(('"' + $folder + '"'), $language) -PassThru
    if (-not $process.WaitForExit(120000)) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        throw ('Tempo massimo superato per ' + $language + '. Le immagini parziali restano nella cartella di destinazione.')
    }
    $process.Refresh()
    $resultPath = Join-Path $folder 'capture-result.json'
    if (-not (Test-Path -LiteralPath $resultPath)) { throw ('Cattura ' + $language + ' non completata. Nessun risultato disponibile.') }
    $result = Get-Content -LiteralPath $resultPath -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($process.ExitCode -ne 0 -or -not $result.success -or $result.language -ne $language) {
        throw ('Cattura ' + $language + ' non completata: ' + $result.message)
    }
    $images = @(Get-ChildItem -LiteralPath $folder -Filter '*.png' -File)
    if ($images.Count -ne 13 -or @($result.screenshots).Count -ne 13) { throw ('Set incompleto per ' + $language) }
    foreach ($image in $images) { if ($image.Length -lt 1024) { throw ('PNG non valido: ' + $image.Name) } }
}
$html = @'
<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width"><title>StagePad Live - Screenshots</title><style>body{margin:0;padding:48px;background:#0b111a;color:#edf4fc;font:20px system-ui}main{max-width:960px;margin:auto}h1,a{color:#75e2c1}nav{display:grid;gap:16px}a{padding:22px;border:1px solid #293c51;border-radius:12px;text-decoration:none}</style><main><h1>StagePad Live - Screenshots</h1><nav><a href="it/index.html" lang="it">Italiano - 13 screenshot</a><a href="en/index.html" lang="en">English - 13 screenshots</a><a href="es/index.html" lang="es">Espa&#241;ol - 13 screenshots</a><a href="fr/index.html" lang="fr">Fran&#231;ais - 13 screenshots</a></nav></main></html>
'@
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'index.html'), $html, $utf8)
Write-Host ('Completato: 52 screenshot nelle cartelle it, en, es, fr. ' + $OutputDirectory)
Start-Process -FilePath (Join-Path $OutputDirectory 'index.html')
