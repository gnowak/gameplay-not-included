# Oxygen Not Included - Gameplay Not Included Telemetry Mod & Companion

Gameplay Not Included is a lightweight, cross-platform telemetry and automation integration mod for **Oxygen Not Included** (ONI). It runs inside the game, periodically extracting rich game state telemetry on the Unity main thread, serializing it to a clean JSON format, writing it to disk, and broadcasting it over a background WebSocket server.

A Python-based live companion console dashboard is included to visualize the colony's metrics in real time.

---

## Features

- **Live Telemetry Dump**: Periodically extracts colony state (every 10 seconds) and writes it to a local JSON file.
- **Cycle History**: Appends historical summaries at the end of every cycle to track growth and performance over time.
- **Background WebSocket Server**: Spawns a high-performance, concurrent, thread-safe WebSocket server (`ws://localhost:8080`) that broadcasts the telemetry live to any connected client.
- **Companion CLI Viewer**: A terminal-based companion dashboard displaying duplicant vitals, food storage, ranched critters, farming crops, and stockpiled resources in real time.

---

## Directory Structure

```text
gameplay-not-included/
├── Mod/
│   ├── src/
│   │   ├── GameplayNotIncludedMod.cs # Entry point and mod initializer
│   │   ├── Patches.cs           # Harmony patches & game clock subscriptions
│   │   ├── ColonyState.cs       # State extraction & file persistence logic
│   │   └── WebSocketServer.cs   # Custom TCP/WebSocket server implementation
│   ├── GameplayNotIncluded.csproj # MSBuild project file
│   └── mod_info.yaml            # Klei mod metadata file
├── agent.py                     # Python companion live console dashboard
├── config.json.template         # Template configuration for agent settings
├── install_mod.ps1              # Automated build and installation script
└── README.md                    # This file
```

---

## Getting Started

### Prerequisites

1. **Oxygen Not Included**
2. **Python 3.x** (only required to run the companion dashboard)

### Installation

The repository includes pre-built binaries and double-clickable installers for easy installation:

#### Windows
1. Double-click the **`install.bat`** file in the repository root.
2. The installer will automatically locate your Klei local mods folder and copy the files.
3. If the folder is not found, the script will prompt you to enter the path to your `OxygenNotIncluded/mods/local/` directory.

#### Linux / Steam Deck / macOS
1. Open your terminal in the repository root.
2. Make the installer executable and run it:
   ```bash
   chmod +x install.sh
   ./install.sh
   ```
3. If auto-detection fails, paste the path to your local mods directory when prompted.

---

### Activating the Mod

1. Launch **Oxygen Not Included**.
2. Click **Mods** in the main menu.
3. Locate **gameplay-not-included** in the list and click **Enable**.
4. Restart the game to apply the changes.

---

## Telemetry Paths & Outputs

Telemetry files are written to the official Klei user data folder to ensure cross-platform compatibility (Windows, macOS, and Linux/Steam Deck):

- **Windows**: `C:\Users\<Username>\Documents\Klei\OxygenNotIncluded\gameplay-not-included\`
- **macOS / Linux**: `~/Documents/Klei/OxygenNotIncluded/gameplay-not-included/`

### Output Files
1. `colony_summary.json`: Contains the latest live snapshot of the colony.
2. `colony_history.json`: Tracks historical stats for every completed cycle.

---

## WebSocket API

The mod runs a WebSocket server on `ws://localhost:8080`. 

- When a new client connects, the server performs the WebSocket handshake.
- Every time a telemetry snapshot is written (every 10 seconds or at cycle end), the JSON content is pushed as a text frame to all connected clients.

---

## Telemetry JSON Schemas

### 1. `colony_summary.json`
```json
{
  "colonyName": "My Asteroid Base",
  "cycle": 42,
  "duplicants": {
    "count": 3,
    "averageStress": 0.5,
    "list": [
      {
        "name": "Ruby",
        "stress": 0.0,
        "calories": 3800000.0,
        "health": 100.0,
        "activeEffect": "None",
        "activeEffects": ["None"]
      }
    ]
  },
  "foodStorage": {
    "totalCalories": 24000.0,
    "items": [
      {
        "id": "MushBar",
        "displayName": "Mush Bar",
        "calories": 8000.0,
        "amountKg": 1.2
      }
    ]
  },
  "critters": [
    {
      "id": "Hatch",
      "wild": 4,
      "domesticHappy": 2,
      "domesticGlum": 0,
      "eggs": 1
    }
  ],
  "crops": [
    {
      "id": "BasicSingleHarvestPlant",
      "wild": 8,
      "domesticated": 4,
      "farmersTouch": 0,
      "planterTypes": {
        "PlanterBox": 4
      }
    }
  ],
  "resources": {
    "Oxygen": 450000.0,
    "Water": 12000000.0,
    "Coal": 8500.0
  }
}
```

### 2. `colony_history.json`
```json
[
  {
    "cycle": 1,
    "duplicants": 3,
    "calories": 12000.0,
    "averageStress": 0.0,
    "powerGenerated": 420.0
  },
  {
    "cycle": 2,
    "duplicants": 3,
    "calories": 24000.0,
    "averageStress": 0.1,
    "powerGenerated": 840.0
  }
]
```

---

## Live Companion Console Dashboard

To run the live console companion dashboard:

1. Open a terminal in the repository root.
2. Run the companion script:
   ```bash
   python agent.py
   ```
3. Command Line Options:
   - `--summary`: Path to `colony_summary.json` (defaults to auto-detected Klei path).
   - `--history`: Path to `colony_history.json` (defaults to auto-detected Klei path).

The terminal UI will automatically refresh and display the vital metrics of your colony!

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Developer Guide - Building from Source

If you want to modify the C# code or rebuild the mod yourself:

1. Ensure you have the **.NET SDK** or MSBuild installed.
2. Run the build & installation script from PowerShell in the repository root:
   ```powershell
   ./install_mod.ps1
   ```
   This will compile the code inside `Mod/` and copy the compiled assembly directly to your game's local mods directory for testing.
