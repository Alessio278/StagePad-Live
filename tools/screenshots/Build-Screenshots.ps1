$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Push-Location $PSScriptRoot
try {
    dotnet run --project StagePad.ScreenshotDataTests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Demo data checks failed.' }
    $destination = Join-Path $repositoryRoot 'artifacts/screenshot-demo'
    dotnet publish StagePad.Screenshots -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o $destination
    if ($LASTEXITCODE -ne 0) { throw 'Screenshot demo build failed.' }
    Move-Item -LiteralPath (Join-Path $destination 'StagePad.exe') -Destination (Join-Path $destination 'StagePad-Screenshots.exe') -Force
    Copy-Item -LiteralPath 'Genera-Screenshot.ps1','Cattura-Screenshot.cmd','SCREENSHOTS-README.txt' -Destination $destination -Force
    Write-Host ('Demo pronta in ' + $destination)
} finally { Pop-Location }
