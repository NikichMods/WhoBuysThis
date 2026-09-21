param(
    [string]$GameDir = $env:GYK_GAME_DIR,
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'

function Add-Candidate([System.Collections.Generic.List[string]]$list, [string]$path) {
    if ([string]::IsNullOrWhiteSpace($path)) { return }
    $full = [Environment]::ExpandEnvironmentVariables($path)
    if (-not $list.Contains($full)) { $list.Add($full) }
}

function Find-GraveyardKeeper([string]$explicitPath) {
    $candidates = New-Object 'System.Collections.Generic.List[string]'
    Add-Candidate $candidates $explicitPath

    if (${env:ProgramFiles(x86)}) {
        Add-Candidate $candidates (Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common\Graveyard Keeper')
    }
    if ($env:ProgramFiles) {
        Add-Candidate $candidates (Join-Path $env:ProgramFiles 'Steam\steamapps\common\Graveyard Keeper')
    }

    $steamRoots = New-Object 'System.Collections.Generic.List[string]'
    try {
        $steamReg = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction Stop).SteamPath
        if ($steamReg) { $steamRoots.Add($steamReg) }
    } catch { }

    foreach ($steamRoot in @($steamRoots)) {
        Add-Candidate $candidates (Join-Path $steamRoot 'steamapps\common\Graveyard Keeper')
        $vdf = Join-Path $steamRoot 'steamapps\libraryfolders.vdf'
        if (-not (Test-Path $vdf)) { continue }
        $text = Get-Content $vdf -Raw
        foreach ($match in [regex]::Matches($text, '"path"\s+"([^"]+)"')) {
            $library = $match.Groups[1].Value.Replace('\\', '\')
            Add-Candidate $candidates (Join-Path $library 'steamapps\common\Graveyard Keeper')
        }
    }

    foreach ($candidate in $candidates) {
        if (Test-Path (Join-Path $candidate 'Graveyard Keeper.exe')) { return $candidate }
    }
    throw 'Graveyard Keeper installation was not found. Set GYK_GAME_DIR to the game folder and run the script again.'
}

function Find-CSharpCompiler {
    $candidates = @(
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
        (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }
    throw '.NET Framework C# compiler (csc.exe) was not found.'
}

$game = Find-GraveyardKeeper $GameDir
$pluginDir = Join-Path $game 'BepInEx\plugins\WhoBuysThisResearch'
$installedDll = Join-Path $pluginDir 'WhoBuysThisResearch.dll'

if ($Uninstall) {
    if (Test-Path $pluginDir) {
        Remove-Item $pluginDir -Recurse -Force
        Write-Host "Removed research harness: $pluginDir"
    } else {
        Write-Host 'Research harness is not installed.'
    }
    exit 0
}

if (Get-Process -Name 'Graveyard Keeper' -ErrorAction SilentlyContinue) {
    throw 'Close Graveyard Keeper before building/installing the research harness.'
}

$bepInEx = Join-Path $game 'BepInEx\core\BepInEx.dll'
$managed = Join-Path $game 'Graveyard Keeper_Data\Managed'
$unityCore = Join-Path $managed 'UnityEngine.CoreModule.dll'
$unityFacade = Join-Path $managed 'UnityEngine.dll'

foreach ($required in @($bepInEx, $unityCore)) {
    if (-not (Test-Path $required)) { throw "Required runtime assembly not found: $required" }
}

$csc = Find-CSharpCompiler
$root = Split-Path $PSScriptRoot -Parent
$buildDir = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
$outDll = Join-Path $buildDir 'WhoBuysThisResearch.dll'

$sources = @(
    (Join-Path $root 'src\WhoBuysThisResearchPlugin.cs'),
    (Join-Path $root 'src\DiagnosticCollector.cs'),
    (Join-Path $root 'src\ReflectionUtil.cs')
)
foreach ($source in $sources) {
    if (-not (Test-Path $source)) { throw "Source file not found: $source" }
}

$compilerArgs = @('/nologo', '/target:library', '/optimize+', "/out:$outDll", "/reference:$bepInEx", "/reference:$unityCore")
if (Test-Path $unityFacade) { $compilerArgs += "/reference:$unityFacade" }
$compilerArgs += $sources

& $csc @compilerArgs
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $outDll)) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Copy-Item $outDll $installedDll -Force

Write-Host ''
Write-Host 'Who Buys This? research harness built and installed.'
Write-Host "Game:      $game"
Write-Host "Installed: $installedDll"
Write-Host ''
Write-Host 'Start the game and load a save. The harness writes:'
Write-Host '  BepInEx\WhoBuysThisResearch\latest-report.txt'
Write-Host '  BepInEx\WhoBuysThisResearch\vendor-catalog.tsv'
Write-Host '  BepInEx\WhoBuysThisResearch\buyer-matrix.tsv'
Write-Host 'It does not modify gameplay or save data.'
