# Eseguire da PowerShell nella cartella estratta: .\Build.ps1
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Installa .NET SDK 10 x64 da https://dotnet.microsoft.com/download/dotnet/10.0 e riapri PowerShell.'
}
& dotnet run --project StagePad.Tests -c Release
if ($LASTEXITCODE -ne 0) { throw 'Test non superati. Pubblicazione annullata.' }
& dotnet run --project StagePad.IntegrationTests -c Release
if ($LASTEXITCODE -ne 0) { throw 'Test di integrazione non superati. Pubblicazione annullata.' }
& dotnet publish StagePad.App/StagePad.App.csproj -c Release -r win-x64 --self-contained true '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' '-p:EnableCompressionInSingleFile=true' -o artifacts/win-x64
if ($LASTEXITCODE -ne 0) { throw 'Compilazione non riuscita.' }
$uiCheck = Start-Process -FilePath "$PSScriptRoot\artifacts\win-x64\StagePad.exe" -ArgumentList '--check-ui-assets' -Wait -PassThru
if ($uiCheck.ExitCode -ne 0) { throw 'Controllo avvio risorse WPF non superato. Pubblicazione annullata.' }
$appVersion = ([xml](Get-Content 'StagePad.App/StagePad.App.csproj')).Project.PropertyGroup.Version
Compress-Archive -Path @('artifacts/win-x64/StagePad.exe','LICENSE','docs/UPDATE.*.md') -DestinationPath "artifacts/StagePad-$appVersion-Windows.zip" -Force
Write-Host "Pronto: $PSScriptRoot\artifacts\win-x64\StagePad.exe"
