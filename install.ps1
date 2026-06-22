# install.ps1
# User-friendly installer for Gameplay Not Included

$ErrorActionPreference = "Stop"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "      Gameplay Not Included Mod Installer      " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# 1. Resolve source path
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDir = Join-Path $scriptDir "dist\gameplay-not-included"

if (-not (Test-Path $sourceDir)) {
    Write-Error "[-] Could not find compiled mod files in '$sourceDir'. Please make sure you have extracted all files from the download."
}

# 2. Try to auto-detect Klei mods folder
$homeDir = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::MyDocuments)
$possiblePaths = @(
    (Join-Path $homeDir "Klei\OxygenNotIncluded\mods\local"),
    (Join-Path $HOME "Documents\Klei\OxygenNotIncluded\mods\local"),
    (Join-Path $HOME "OneDrive\Documents\Klei\OxygenNotIncluded\mods\local")
)

# Registry fallback for customized User Documents folders
try {
    $regPath = Get-ItemPropertyValue -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders" -Name "Personal" -ErrorAction SilentlyContinue
    if ($regPath) {
        $resolvedRegPath = [System.Environment]::ExpandEnvironmentVariables($regPath)
        $possiblePaths += (Join-Path $resolvedRegPath "Klei\OxygenNotIncluded\mods\local")
    }
} catch {}

$modsDir = $null
foreach ($path in $possiblePaths) {
    if (Test-Path $path) {
        $modsDir = $path
        break
    }
}

# 3. Fallback: Prompt user if not auto-detected
if ($null -eq $modsDir) {
    Write-Host "[!] Could not automatically locate the Oxygen Not Included mods folder." -ForegroundColor Yellow
    Write-Host "We checked standard locations under Documents."
    Write-Host ""
    
    $promptLoop = $true
    while ($promptLoop) {
        $userInput = Read-Host "Please enter the folder path to your 'OxygenNotIncluded\mods\local' directory"
        if ($userInput) {
            $userInput = $userInput.Trim()
            # Support quoting
            $userInput = $userInput.Trim('"')
            $userInput = $userInput.Trim("'")
            
            if (Test-Path $userInput) {
                $modsDir = $userInput
                $promptLoop = $false
            } else {
                Write-Host "[-] The path you entered does not exist: '$userInput'. Please try again." -ForegroundColor Red
            }
        } else {
            Write-Host "[-] Path cannot be empty." -ForegroundColor Red
        }
    }
}

# 4. Perform the installation
$targetDir = Join-Path $modsDir "gameplay-not-included"
Write-Host "[*] Target installation folder: $targetDir" -ForegroundColor Cyan

try {
    if (-not (Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }
    
    Write-Host "[*] Copying mod files..." -ForegroundColor Cyan
    Copy-Item -Path (Join-Path $sourceDir "*") -Destination $targetDir -Force -Recurse
    
    Write-Host ""
    Write-Host "[+] Installation Successful!" -ForegroundColor Green
    Write-Host "[+] The mod has been installed into your game's local mods list." -ForegroundColor Green
    Write-Host "[+] To enable it, launch Oxygen Not Included, click 'Mods' in the main menu, and enable 'gameplay-not-included'." -ForegroundColor Green
} catch {
    Write-Host "[-] Installation failed: $_" -ForegroundColor Red
}
Write-Host ""
