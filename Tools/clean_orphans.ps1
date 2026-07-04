param (
    [string]$TargetDir = "..\Game"
)

$targetPath = Resolve-Path $TargetDir -ErrorAction Stop
Write-Host "Scanning for orphaned .uid and .import files in $targetPath..."

$extensions = @("*.uid", "*.import")
$orphanedCount = 0

foreach ($ext in $extensions) {
    $files = Get-ChildItem -Path $targetPath -Filter $ext -Recurse -File
    foreach ($file in $files) {
        # Extract the original filename by stripping the .uid or .import extension
        $originalFileName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $originalFilePath = Join-Path -Path $file.DirectoryName -ChildPath $originalFileName
        
        # If the original file does not exist, this is an orphan
        if (-not (Test-Path -Path $originalFilePath)) {
            Write-Host "Deleting orphaned file: $($file.FullName)"
            Remove-Item -Path $file.FullName -Force
            $orphanedCount++
        }
    }
}

Write-Host "Cleanup complete! Removed $orphanedCount orphaned files."
