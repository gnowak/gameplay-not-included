#!/bin/bash
# install.sh
# User-friendly installer for Gameplay Not Included on Linux/macOS

echo "============================================="
echo "      Gameplay Not Included Mod Installer      "
echo "============================================="
echo ""

# 1. Resolve source path
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOURCE_DIR="$SCRIPT_DIR/dist/gameplay-not-included"

if [ ! -d "$SOURCE_DIR" ]; then
    echo "[-] Error: Could not find compiled mod files in '$SOURCE_DIR'."
    echo "[-] Please make sure you have extracted all files from the download."
    exit 1
fi

# 2. Try to auto-detect Klei mods folder
POSSIBLE_PATHS=(
    # macOS
    "$HOME/Library/Application Support/unity.Klei.Oxygen Not Included/mods/local"
    # Linux Native
    "$HOME/.config/unity3d/Klei/Oxygen Not Included/mods/local"
    # Steam Deck / Proton (standard path)
    "$HOME/.local/share/Steam/steamapps/compatdata/251110/pfx/drive_c/users/steamuser/Documents/Klei/OxygenNotIncluded/mods/local"
    # Alternative Steam Deck / Proton path
    "$HOME/.steam/steam/steamapps/compatdata/251110/pfx/drive_c/users/steamuser/Documents/Klei/OxygenNotIncluded/mods/local"
)

MODS_DIR=""
for path in "${POSSIBLE_PATHS[@]}"; do
    if [ -d "$path" ]; then
        MODS_DIR="$path"
        break
    fi
done

# 3. Fallback: Prompt user if not auto-detected
if [ -z "$MODS_DIR" ]; then
    echo "[!] Could not automatically locate the Oxygen Not Included mods folder."
    echo "We checked standard macOS and Steam Deck/Linux directories."
    echo ""
    
    while true; do
        read -p "Please enter the folder path to your 'OxygenNotIncluded/mods/local' directory: " user_input
        if [ -n "$user_input" ]; then
            # Expand tilde ~ if present
            expanded_path="${user_input/#\~/$HOME}"
            
            if [ -d "$expanded_path" ]; then
                MODS_DIR="$expanded_path"
                break
            else
                echo "[-] The path you entered does not exist: '$expanded_path'. Please try again."
            fi
        else
            echo "[-] Path cannot be empty."
        fi
    done
fi

# 4. Perform the installation
TARGET_DIR="$MODS_DIR/gameplay-not-included"
echo "[*] Target installation folder: $TARGET_DIR"

if [ ! -d "$TARGET_DIR" ]; then
    mkdir -p "$TARGET_DIR"
fi

echo "[*] Copying mod files..."
cp -R "$SOURCE_DIR/"* "$TARGET_DIR/"

echo ""
echo "[+] Installation Successful!"
echo "[+] The mod has been installed into your game's local mods list."
echo "[+] To enable it, launch Oxygen Not Included, click 'Mods' in the main menu, and enable 'gameplay-not-included'."
echo ""
