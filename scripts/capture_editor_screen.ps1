<#
.SYNOPSIS
Opens one RacingGameCasaEngine screen asset in the CasaEngine editor through the editor's automation command line,
and captures the editor window (ADR-0002).

.DESCRIPTION
Runs CasaEngine.Editor.exe on the editor project RacingGameCasaEngine/Content/RacingGameCasaEngine.json with
--open-asset <Screen>, --entity-index 0, --capture-delay, --diagnostics-out and --screenshot-out, plus optionally
--set-screen-property and --save-project. The editor writes the capture as <screen>-final.png.

The editor automation only proceeds once the start world (Worlds/Editor.world) holds an entity, and some failures make
it run forever without capture or exception (a world without entity; an asset that cannot be opened followed by
--set-screen-property). The wait is therefore bounded by -TimeoutSeconds. On expiry, the editor process started by
this script, and only that one, is stopped.

Exit codes: 0 success; 1 failure (editor exit code, diagnostics file missing or reporting an automation failure,
asset not opened, capture missing); 2 timeout.

.EXAMPLE
pwsh scripts/capture_editor_screen.ps1 -Screen UI/Screens/Splash/Splash.uiscreen
#>
param(
    # Screen asset path, relative to the project root (as catalogued in AssetInfos.json) or absolute.
    [Parameter(Mandatory = $true)][string]$Screen,
    # Editor project root (folder of RacingGameCasaEngine.json and AssetInfos.json). Point it at a copy to edit safely.
    # Default: RacingGameCasaEngine/Content.
    [string]$ProjectRoot = '',
    # Folder receiving the capture and the diagnostics. Default: a new folder under the temporary directory.
    [string]$OutputDirectory = '',
    # Default: the editor built from the CasaEngine submodule (Debug).
    [string]$EditorExe = '',
    [double]$CaptureDelaySeconds = 3,
    [int]$TimeoutSeconds = 120,
    # <NodeName>:<Property>=<Value>, applied to the opened screen before the capture.
    [string]$SetScreenProperty = '',
    # Saves the project after the edit (File > Save: screens, world, project file and AssetInfos.json).
    [switch]$SaveProject
)

$ErrorActionPreference = 'Stop'

# Windows PowerShell 5.1 leaves $PSScriptRoot empty in parameter defaults: resolve the defaults here.
$repositoryRoot = Split-Path -Parent $PSScriptRoot
if (-not $ProjectRoot)
{
    $ProjectRoot = Join-Path $repositoryRoot 'RacingGameCasaEngine\Content'
}

if (-not $EditorExe)
{
    $EditorExe = Join-Path $repositoryRoot 'CasaEngine\CasaEngine.Editor\bin\Debug\net9.0-windows\CasaEngine.Editor.exe'
}

# Quotes one command-line argument for the Windows argument parser.
function ConvertTo-CommandLineArgument([string]$value)
{
    if ($value -notmatch '[\s"]' -and $value.Length -gt 0)
    {
        return $value
    }

    $escaped = $value -replace '(\\*)"', '$1$1\"'
    $escaped = $escaped -replace '(\\+)$', '$1$1'
    return '"' + $escaped + '"'
}

if (-not (Test-Path -LiteralPath $EditorExe))
{
    Write-Host "Editor not found: $EditorExe (build CasaEngine/CasaEngine.Editor.MonoGame.sln first)"
    exit 1
}

$projectRootPath = (Resolve-Path -LiteralPath $ProjectRoot).Path
$projectFile = Join-Path $projectRootPath 'RacingGameCasaEngine.json'
if (-not (Test-Path -LiteralPath $projectFile))
{
    Write-Host "Project file not found: $projectFile"
    exit 1
}

$stem = [IO.Path]::GetFileNameWithoutExtension($Screen)
if (-not $OutputDirectory)
{
    $OutputDirectory = Join-Path ([IO.Path]::GetTempPath()) ('rgce-editor-capture\' + (Get-Date -Format 'yyyyMMdd-HHmmss') + "-$stem")
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$outputPath = (Resolve-Path -LiteralPath $OutputDirectory).Path
$screenshotBasePath = Join-Path $outputPath "$stem.png"
$capturePath = Join-Path $outputPath "$stem-final.png"
$diagnosticsPath = Join-Path $outputPath "$stem.diagnostics.txt"
Remove-Item -LiteralPath $capturePath, $diagnosticsPath -ErrorAction SilentlyContinue

# The editor writes some state relative to its current directory: run it from a folder of its own.
$workingDirectory = Join-Path ([IO.Path]::GetTempPath()) ('rgce-editor-cwd-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workingDirectory | Out-Null

$arguments = @(
    '--project', $projectFile,
    '--open-asset', $Screen,
    '--entity-index', '0',
    '--capture-delay', $CaptureDelaySeconds.ToString([Globalization.CultureInfo]::InvariantCulture),
    '--diagnostics-out', $diagnosticsPath,
    '--screenshot-out', $screenshotBasePath)
if ($SetScreenProperty)
{
    $arguments += @('--set-screen-property', $SetScreenProperty)
}

if ($SaveProject)
{
    $arguments += '--save-project'
}

$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = (Resolve-Path -LiteralPath $EditorExe).Path
$startInfo.Arguments = ($arguments | ForEach-Object { ConvertTo-CommandLineArgument $_ }) -join ' '
$startInfo.WorkingDirectory = $workingDirectory
$startInfo.UseShellExecute = $false

Write-Host "Editor: $($startInfo.FileName) $($startInfo.Arguments)"
$process = [System.Diagnostics.Process]::Start($startInfo)
try
{
    if (-not $process.WaitForExit($TimeoutSeconds * 1000))
    {
        $process.Kill()
        $process.WaitForExit()
        Write-Host "TIMEOUT: the editor did not exit within $TimeoutSeconds s and was stopped (process $($process.Id))."
        Write-Host "Diagnostics: $diagnosticsPath"
        exit 2
    }

    $exitCode = $process.ExitCode
}
finally
{
    $process.Dispose()
    Remove-Item -LiteralPath $workingDirectory -Recurse -Force -ErrorAction SilentlyContinue
}

$failures = @()
if ($exitCode -ne 0)
{
    $failures += "editor exit code $exitCode"
}

if (-not (Test-Path -LiteralPath $diagnosticsPath))
{
    $failures += 'diagnostics file missing'
}
else
{
    $diagnostics = Get-Content -LiteralPath $diagnosticsPath
    # Program.Main writes this header, then the exception, into the --diagnostics-out file when the editor throws.
    if ($diagnostics.Count -gt 0 -and $diagnostics[0] -eq 'CasaEngine Editor automation failure')
    {
        $failures += 'editor automation failure reported in the diagnostics'
    }

    if ($diagnostics | Select-String -SimpleMatch '[Automation] Unable to open asset')
    {
        $failures += "screen asset '$Screen' not opened"
    }

    $diagnostics | Select-String -SimpleMatch '[Automation]' | ForEach-Object { Write-Host "  $($_.Line)" }
}

if (-not (Test-Path -LiteralPath $capturePath))
{
    $failures += 'capture missing'
}

Write-Host "Capture: $capturePath"
Write-Host "Diagnostics: $diagnosticsPath"
if ($failures.Count -gt 0)
{
    Write-Host ('FAILED: ' + ($failures -join '; '))
    exit 1
}

Write-Host 'OK'
exit 0
