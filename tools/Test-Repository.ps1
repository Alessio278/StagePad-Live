$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $files = if ((Get-Command git -ErrorAction SilentlyContinue) -and (Test-Path '.git')) {
        @(& git ls-files --cached --others --exclude-standard) | Sort-Object -Unique
    } else {
        Get-ChildItem -File -Recurse -Force | Where-Object { $_.FullName -notmatch '[\\/](\.git|bin|obj|artifacts)[\\/]' } | ForEach-Object { $( $_.FullName.Substring($root.TrimEnd([char[]]"\/").Length + 1) ) }
    }
    $textExtensions = @('.cs','.csproj','.props','.json','.xaml','.md','.txt','.yml','.yaml','.ps1','.cmd','.py','.svg')
    foreach ($file in $files) {
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { continue }
        if ([IO.Path]::GetExtension($file) -notin $textExtensions) { continue }
        $text = [IO.File]::ReadAllText((Join-Path $root $file))
        if ($text.Contains([char]0x2014) -or $text.Contains([char]0x2013)) { throw "Long dash punctuation in $file" }
        if ($text -match 'ghp_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|-----BEGIN [A-Z ]*PRIVATE KEY-----') { throw "Possible secret in $file" }
        if ([IO.Path]::GetExtension($file) -eq '.md') {
            $links = [regex]::Matches($text, '\[[^\]]*\]\(([^)]+)\)')
            foreach ($link in $links) {
                $target = $link.Groups[1].Value.Trim('<','>')
                if ($target -match '^(https?://|mailto:|#)') { continue }
                $path = [Uri]::UnescapeDataString(($target -split '#',2)[0])
                if ($path -and -not (Test-Path -LiteralPath (Join-Path (Split-Path (Join-Path $root $file) -Parent) $path))) { throw "Broken local link in ${file}: $target" }
            }
        }
    }
    $version = ([xml](Get-Content 'StagePad.App/StagePad.App.csproj' -Raw)).Project.PropertyGroup.Version
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Stable semantic version required.' }
    if (-not (Test-Path "docs/releases/v$version.md")) { throw 'Release notes missing.' }
    foreach ($file in @('.gitignore','.github/workflows/ci.yml','.github/workflows/release.yml','.github/ISSUE_TEMPLATE/bug_report.yml','LICENSE','README.md','SECURITY.md')) {
        if (-not (Test-Path $file)) { throw "Required repository file missing: $file" }
    }
    Write-Host "Repository validation passed: $($files.Count) files; version $version."
} finally { Pop-Location }
