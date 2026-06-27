<#
.SYNOPSIS
    Builds historical versions of the Godot project for the last N git commits.

.DESCRIPTION
    This script automates compiling old versions of the project. It creates a temporary clone,
    checks out the requested commits, patches UIDs to prevent breakage, builds the C# and Godot files,
    and exports them into unique folders inside 'bin\history'.

.PARAMETER GodotExe
    Path to the Godot 4 executable. If omitted, the script automatically searches your PATH and Program Files.

.PARAMETER CommitCount
    The number of recent Git commits to build. Defaults to 1.

.PARAMETER ExportType
    The type of export ('Debug' or 'Release'). Defaults to 'Debug'.

.EXAMPLE
    .\tools\build_history.ps1
    Builds the debug version of the last commit automatically.

.EXAMPLE
    .\tools\build_history.ps1 -CommitCount 5 -ExportType "Release"
    Builds the release versions of the last 5 commits.
#>
param(
    [string]$GodotExe = "",
    [int]$CommitCount = 1,
    [ValidateSet("Debug", "Release")]
    [string]$ExportType = "Debug"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($GodotExe)) {
    $godotInPath = Get-Command godot -ErrorAction SilentlyContinue
    if ($godotInPath) {
        $GodotExe = $godotInPath.Source
    } else {
        Write-Host "Searching for Godot executable..."
        $GodotExe = Get-ChildItem -Path "C:\Program Files", "C:\Program Files (x86)" -Filter "Godot*mono*.exe" -Recurse -Depth 2 -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty FullName
    }
    
    if (-not $GodotExe) {
        Write-Error "Could not find Godot executable! Please install it or provide it via -GodotExe."
        exit 1
    }
}

Write-Host "Using Godot Executable: $GodotExe"

$rootDir = $PSScriptRoot | Split-Path -Parent
$binDir = Join-Path $rootDir "bin"
$historyDir = Join-Path $binDir "history"
$tmpDir = Join-Path $binDir "tmp"

Write-Host "Setting up history directory: $historyDir"
if (Test-Path $historyDir) {
    cmd.exe /c "rmdir /s /q `"$historyDir`""
}
New-Item -ItemType Directory -Force -Path $historyDir | Out-Null
New-Item -ItemType File -Force -Path (Join-Path $historyDir ".gdignore") | Out-Null

$commits = git -C $rootDir log -n $CommitCount --format="%H"

foreach ($commit in $commits) {
    Write-Host "=========================================="
    Write-Host "Processing commit: $commit"

    if (Test-Path $tmpDir) {
        cmd.exe /c "rmdir /s /q `"$tmpDir`""
    }

    Write-Host "Cloning repo to $tmpDir ..."
    git clone $rootDir $tmpDir

    Push-Location $tmpDir
    try {
        Write-Host "Checking out commit $commit ..."
        git checkout $commit

        [xml]$xml = Get-Content "version.props"
        $vMajor = $xml.Project.PropertyGroup.VersionMajor
        $vMinor = $xml.Project.PropertyGroup.VersionMinor
        $vBuild = $xml.Project.PropertyGroup.VersionBuild
        $version = "$vMajor.$vMinor.$vBuild-$($commit.Substring(0,7))"
        Write-Host "Found version: $version"

        $targetDir = Join-Path $historyDir $version
        New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

        Write-Host "Copying export_presets.cfg from main repo..."
        Copy-Item (Join-Path $rootDir "export_presets.cfg") -Destination . -Force

        Write-Host "Patching project.godot to fix broken UIDs from old commits..."
        $projectConfig = Get-Content "project.godot"
        $projectConfig = $projectConfig -replace 'uid://bujvwvf4xmwo0', 'res://scenes/game.tscn'
        $projectConfig = $projectConfig -replace '\*uid://dl7mukxiiwvoo', '*res://scripts/PaintingLibrary.cs'
        Set-Content "project.godot" $projectConfig

        Write-Host "Building C# project first to satisfy Godot autoloads..."
        $dotnetProc = Start-Process -FilePath "dotnet" -ArgumentList "build" -PassThru -NoNewWindow
        $dotnetProc.WaitForExit(60000)

        Write-Host "Importing assets for Godot..."
        $importProc = Start-Process -FilePath $GodotExe -ArgumentList "--path .","--headless","--editor","--quit" -PassThru -NoNewWindow
        $importProc.WaitForExit(60000)
        if (-not $importProc.HasExited) {
            Write-Host "Import process hung. Killing it..."
            $importProc.Kill()
        }

        $exportExe = Join-Path $targetDir "Chromonia.exe"
        Write-Host "Building Godot project to $exportExe ..."

        $exportFlag = if ($ExportType -eq "Debug") { "--export-debug" } else { "--export-release" }
        $exportProc = Start-Process -FilePath $GodotExe -ArgumentList "--path .","--headless",$exportFlag,"`"Windows Desktop`"","`"$exportExe`"" -PassThru -NoNewWindow
        $exportProc.WaitForExit(90000)
        if (-not $exportProc.HasExited) {
            Write-Host "Export process hung (common Godot bug on exit). Killing it..."
            $exportProc.Kill()
        }

        Write-Host "Copying paitings folder to the build directory..."
        # We copy from the main repo's root directory so we get the complete set
        $sourcePaitings = Join-Path $rootDir "paitings"
        $targetPaitings = Join-Path $targetDir "paitings"
        Copy-Item -Recurse -Force $sourcePaitings -Destination $targetPaitings

        Write-Host "Successfully built version $version"
    }
    catch {
        Write-Host "Error building commit $commit : $_"
    }
    finally {
        Pop-Location
    }
}

Write-Host "=========================================="
Write-Host "Cleaning up tmp directory..."
if (Test-Path $tmpDir) {
    cmd.exe /c "rmdir /s /q `"$tmpDir`""
}

Write-Host "All done! History builds are located in $historyDir"
