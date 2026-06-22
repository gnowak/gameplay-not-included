# install_mod.ps1
# Script to build and copy the Gameplay Not Included mod to the game's local mods directory

$ErrorActionPreference = "Stop"

# 1. Build the mod using dotnet build
Write-Host "[*] Building the mod..." -ForegroundColor Cyan
dotnet build Mod/GameplayNotIncluded.csproj | Out-Null

$sourceDll = "Mod\bin\Debug\GameplayNotIncluded.dll"
$sourceYaml = "Mod\mod_info.yaml"

if (-not (Test-Path $sourceDll)) {
    Write-Error "[-] Compiled DLL not found at $sourceDll. Build might have failed."
}

# 2. Define the destination folder in Klei local mods
$destinationDir = Join-Path $HOME "Documents\Klei\OxygenNotIncluded\mods\local\gameplay-not-included"

Write-Host "[*] Target directory: $destinationDir" -ForegroundColor Cyan

# 3. Create the directory if it doesn't exist
if (-not (Test-Path $destinationDir)) {
    New-Item -ItemType Directory -Force -Path $destinationDir | Out-Null
    Write-Host "[+] Created target directory." -ForegroundColor Green
}

# 4. Copy the files
Write-Host "[*] Copying mod files..." -ForegroundColor Cyan
Copy-Item -Path $sourceDll -Destination $destinationDir -Force
Copy-Item -Path $sourceYaml -Destination $destinationDir -Force

Write-Host "[+] Mod installed successfully! Load Oxygen Not Included to enable it." -ForegroundColor Green
