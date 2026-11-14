# Windows build: validation, tests, native UI checks and portable package.
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if (-not $IsWindows -and $PSVersionTable.PSEdition -eq 'Core') { throw 'Build.ps1 requires Windows for native WPF checks.' }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Install .NET SDK 10 x64, then reopen PowerShell.' }
New-Item 'artifacts/validation' -ItemType Directory -Force | Out-Null
& "$PSScriptRoot/tools/Test-Repository.ps1"
& dotnet run --project StagePad.Tests -c Release | Tee-Object 'artifacts/validation/core-tests.log'
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed. Packaging cancelled.' }
& dotnet run --project StagePad.IntegrationTests -c Release | Tee-Object 'artifacts/validation/integration-tests.log'
if ($LASTEXITCODE -ne 0) { throw 'Integration tests failed. Packaging cancelled.' }
& dotnet publish StagePad.App/StagePad.App.csproj -c Release -r win-x64 --self-contained true '-p:PublishSingleFile=true' '-p:IncludeNativeLibrariesForSelfExtract=true' '-p:EnableCompressionInSingleFile=true' -o artifacts/win-x64
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
$previousDataRoot = $env:STAGEPAD_DATA_DIR
$smokeRoot = Join-Path ([IO.Path]::GetTempPath()) ('StagePad-ui-check-' + [Guid]::NewGuid().ToString('N'))
try {
    $env:STAGEPAD_DATA_DIR = $smokeRoot
    $uiCheck = Start-Process -FilePath "$PSScriptRoot/artifacts/win-x64/StagePad.exe" -ArgumentList '--check-ui-assets' -PassThru
    if (-not $uiCheck.WaitForExit(45000)) { $uiCheck.Kill(); throw 'Native WPF check timed out.' }
    if ($uiCheck.ExitCode -ne 0) {
        if (Test-Path "$smokeRoot/ui-check-error.txt") { Get-Content "$smokeRoot/ui-check-error.txt" | Write-Host }
        throw 'Native WPF startup/rendering checks failed. Packaging cancelled.'
    }
    if (-not (Test-Path "$smokeRoot/ui-check-success.json")) { throw 'Native WPF success record missing.' }
    Copy-Item "$smokeRoot/ui-check-success.json" 'artifacts/validation/windows-ui.json'
} finally {
    $env:STAGEPAD_DATA_DIR = $previousDataRoot
    if (Test-Path $smokeRoot) { Remove-Item $smokeRoot -Recurse -Force -ErrorAction SilentlyContinue }
}
$version = ([xml](Get-Content 'StagePad.App/StagePad.App.csproj')).Project.PropertyGroup.Version
$exe = 'artifacts/win-x64/StagePad.exe'
$hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $PSScriptRoot 'artifacts/win-x64/SHA256.txt'), "$hash  StagePad.exe" + [Environment]::NewLine, [Text.UTF8Encoding]::new($false))
Copy-Item 'LICENSE' 'artifacts/win-x64/LICENSE'
Copy-Item 'docs/UPDATE.en.md' 'artifacts/win-x64/UPDATE.en.md'
Copy-Item 'docs/UPDATE.it.md' 'artifacts/win-x64/UPDATE.it.md'
Copy-Item 'docs/UPDATE.es.md' 'artifacts/win-x64/UPDATE.es.md'
Copy-Item 'docs/UPDATE.fr.md' 'artifacts/win-x64/UPDATE.fr.md'
Copy-Item 'docs/TEST-COMPLETI.md' 'artifacts/win-x64/WINDOWS-CHECKLIST.md'
Compress-Archive -Path 'artifacts/win-x64/*' -DestinationPath "artifacts/StagePad-Live-${version}-Windows.zip" -Force
Copy-Item 'artifacts/win-x64/SHA256.txt' 'artifacts/SHA256.txt'
@"
# StagePad Live $version - release checks

- Environment: Windows x64, SDK selected by global.json.
- Repository validation: passed.
- Core and simulated integration tests: passed; logs in artifacts/validation.
- Native WPF startup/rendering checks: passed; see validation/windows-ui.json.
- Real audio/MIDI hardware and complete long-show acceptance: not implied by these checks.
- Code signing: not provided.
"@ | Set-Content 'artifacts/RELEASE-CHECKS.md' -Encoding utf8
Write-Host "Ready: StagePad Live $version"
