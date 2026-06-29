# SPDX-FileCopyrightText: 2026 Juan Medina
# SPDX-License-Identifier: MIT
#
# Increments the build number in version.props and syncs the new version
# into project.godot so both the .NET assembly and the Godot export carry
# the same version string.
#
# Usage (called automatically by MSBuild BeforeBuild target):
#   pwsh tools/bump_version.ps1

$ErrorActionPreference = 'Stop'

$repoRoot       = Resolve-Path "$PSScriptRoot/../../Game"
$versionProps   = Join-Path $repoRoot "version.props"
$projectGodot   = Join-Path $repoRoot "project.godot"

# --- Validate required files exist ---
if (-not (Test-Path $versionProps)) {
    Write-Error "version.props not found at $versionProps"
    exit 1
}
if (-not (Test-Path $projectGodot)) {
    Write-Error "project.godot not found at $projectGodot"
    exit 1
}

# --- Read and increment build number in version.props ---
[xml]$xml = Get-Content $versionProps -Raw
$major    = $xml.Project.PropertyGroup.VersionMajor
$minor    = $xml.Project.PropertyGroup.VersionMinor
$build    = [int]$xml.Project.PropertyGroup.VersionBuild

$newBuild = $build + 1
$version  = "$major.$minor.$newBuild"

$xml.Project.PropertyGroup.VersionBuild = "$newBuild"
$xml.Save($versionProps)

# Normalize line endings written by XmlDocument.Save()
$content = Get-Content $versionProps -Raw
$content = $content -replace "`r`n", "`n"
[System.IO.File]::WriteAllText($versionProps, $content, [System.Text.UTF8Encoding]::new($false))

Write-Host "Version bumped to $version"

# --- Sync version into project.godot ---
$lines       = Get-Content $projectGodot
$versionLine = "config/version=`"$version`""
$found       = $false

$updated = $lines | ForEach-Object {
    if ($_ -match '^config/version=') {
        $versionLine
        $found = $true
    } else {
        $_
    }
}

# If the line didn't exist yet, insert it after config/name= under [application]
if (-not $found) {
    $updated = $lines | ForEach-Object {
        $_
        if ($_ -match '^config/name=') {
            $versionLine
        }
    }
}

# Write with LF line endings, no BOM
$contentToSave = ($updated -join "`n") + "`n"
[System.IO.File]::WriteAllText($projectGodot, $contentToSave, [System.Text.UTF8Encoding]::new($false))

Write-Host "project.godot updated to $version"