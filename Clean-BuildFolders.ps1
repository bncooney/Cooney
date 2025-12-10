# Clean-BuildFolders.ps1
# Deletes all bin and obj folders from the repository (Easy way out to clean up NuGet package builds)

$ErrorActionPreference = "Stop"

Write-Host "Cleaning build folders (bin and obj) from repository..." -ForegroundColor Cyan

# Get the script's directory (repository root)
$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path

# Find and delete all bin folders
$binFolders = Get-ChildItem -Path $repoRoot -Directory -Recurse -Filter "bin" -ErrorAction SilentlyContinue
$binCount = $binFolders.Count

if ($binCount -gt 0) {
    Write-Host "Found $binCount bin folder(s):" -ForegroundColor Yellow
    foreach ($folder in $binFolders) {
        Write-Host "  Deleting: $($folder.FullName)" -ForegroundColor Gray
        Remove-Item -Path $folder.FullName -Recurse -Force
    }
    Write-Host "Deleted $binCount bin folder(s)" -ForegroundColor Green
} else {
    Write-Host "No bin folders found" -ForegroundColor Gray
}

# Find and delete all obj folders
$objFolders = Get-ChildItem -Path $repoRoot -Directory -Recurse -Filter "obj" -ErrorAction SilentlyContinue
$objCount = $objFolders.Count

if ($objCount -gt 0) {
    Write-Host "Found $objCount obj folder(s):" -ForegroundColor Yellow
    foreach ($folder in $objFolders) {
        Write-Host "  Deleting: $($folder.FullName)" -ForegroundColor Gray
        Remove-Item -Path $folder.FullName -Recurse -Force
    }
    Write-Host "Deleted $objCount obj folder(s)" -ForegroundColor Green
} else {
    Write-Host "No obj folders found" -ForegroundColor Gray
}

Write-Host "`nCleanup complete! Deleted $($binCount + $objCount) folder(s) total." -ForegroundColor Cyan
