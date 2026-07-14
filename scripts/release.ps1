# SPDX-FileCopyrightText: 2026 Juan Medina
# SPDX-License-Identifier: MIT

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path "$PSScriptRoot/.."

# --- Check required tools ---

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
	Write-Error "git is not installed or not in PATH."
	exit 1
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
	Write-Error "GitHub CLI (gh) is not installed or not in PATH. See https://cli.github.com"
	exit 1
}

# --- Check required files ---

$versionPropsPath = Join-Path $repoRoot "Game/version.props"
if (-not (Test-Path $versionPropsPath)) {
	Write-Error "version.props not found at $versionPropsPath"
	exit 1
}

$releaseNotesPath = Join-Path $repoRoot "Game/release-notes.md"
if (-not (Test-Path $releaseNotesPath)) {
	Write-Error "release-notes.md not found at $releaseNotesPath"
	exit 1
}

# --- Check for uncommitted changes ---

$status = git -C $repoRoot status --porcelain
if ($status) {
	Write-Error "There are uncommitted changes. Please commit or stash them before releasing.`n$status"
	exit 1
}

# --- Check for unpushed commits ---

$unpushed = git -C $repoRoot log --oneline "@{u}..HEAD" 2>$null
if ($unpushed) {
	Write-Error "There are unpushed commits. Please push before releasing.`n$unpushed"
	exit 1
}

# --- Read version ---

[xml]$xml = Get-Content $versionPropsPath
$major = $xml.Project.PropertyGroup.VersionMajor
$minor = $xml.Project.PropertyGroup.VersionMinor
$build = $xml.Project.PropertyGroup.VersionBuild
$tag = "v$major.$minor.$build"

Write-Host "Version: $tag"

# --- Check tag does not already exist ---

$existingTag = git -C $repoRoot tag -l $tag
if ($existingTag) {
	Write-Error "Tag $tag already exists. Bump the version before releasing."
	exit 1
}

# --- Create and push tag ---

Write-Host "Creating tag $tag..."
git -C $repoRoot tag -a $tag -m "Release $tag"
if ($LASTEXITCODE -ne 0) {
	Write-Error "Failed to create git tag."
	exit 1
}

Write-Host "Pushing tag..."
git -C $repoRoot push --tags
if ($LASTEXITCODE -ne 0) {
	Write-Error "Failed to push tags."
	exit 1
}

# --- Create GitHub release ---

Write-Host "Creating GitHub release $tag..."
gh release create $tag -F $releaseNotesPath -t $tag --repo (git -C $repoRoot remote get-url origin)
if ($LASTEXITCODE -ne 0) {
	Write-Error "Failed to create GitHub release."
	exit 1
}

Write-Host "Release $tag created successfully."
