import os
import sys
import json
import time
import argparse

def get_default_path(filename):
    home = os.path.expanduser("~")
    paths_to_try = [
        os.path.join(home, "Documents", "Klei", "OxygenNotIncluded", "gameplay-not-included"),
        os.path.join(home, "OneDrive", "Documents", "Klei", "OxygenNotIncluded", "gameplay-not-included"),
    ]
    if os.name == 'nt':
        try:
            import winreg
            with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders") as key:
                personal_path, _ = winreg.QueryValueEx(key, "Personal")
                personal_path = os.path.expandvars(personal_path)
                paths_to_try.insert(0, os.path.join(personal_path, "Klei", "OxygenNotIncluded", "gameplay-not-included"))
        except Exception:
            pass
    paths_to_try.append(r"C:\ONI_Agent")
    
    for p in paths_to_try:
        if os.path.exists(p):
            return os.path.join(p, filename)
    return os.path.join(paths_to_try[0], filename)

class ONILiveViewer:
    def __init__(self, summary_path=None, history_path=None):
        self.summary_path = summary_path or get_default_path("colony_summary.json")
        self.history_path = history_path or get_default_path("colony_history.json")

    def read_json(self, path):
        if not os.path.exists(path):
            return None
        try:
            with open(path, "r") as f:
                return json.load(f)
        except Exception:
            return None

    def render(self):
        summary = self.read_json(self.summary_path)
        history = self.read_json(self.history_path)

        if not summary:
            # Print wait message
            os.system('cls' if os.name == 'nt' else 'clear')
            print("=" * 80)
            print(" OXYGEN NOT INCLUDED - LIVE COLONY COMPANION VIEW")
            print("=" * 80)
            print(f"\n[*] Waiting for live telemetry at {self.summary_path}...")
            print("[*] Please load a save game in Oxygen Not Included with the mod active.")
            return

        # Clear screen
        os.system('cls' if os.name == 'nt' else 'clear')

        print("=" * 80)
        print(f" OXYGEN NOT INCLUDED - LIVE COLONY COMPANION: {summary.get('colonyName', 'Unknown').upper()}")
        print("=" * 80)

        # 1. Meta / Vitals Panel
        cycle = summary.get("cycle", 0)
        dups = summary.get("duplicants", {})
        dup_count = dups.get("count", 0)
        avg_stress = dups.get("averageStress", 0.0)
        food = summary.get("foodStorage", {})
        total_kcal = food.get("totalCalories", 0.0) / 1000.0

        # Calculate history stats if available
        last_power = 0.0
        if history and len(history) > 0:
            last_entry = history[-1]
            last_power = last_entry.get("powerGenerated", 0.0) / 1000.0 # kJ

        print(f" Cycle: {cycle:<5} | Duplicants: {dup_count:<5} | Food Reserves: {total_kcal:,.0f} kcal | Stress: {avg_stress:.1f}%")
        print(f" Power Generated (Last Cycle): {last_power:.1f} kJ")
        print("-" * 80)

        # 2. Duplicants Panel
        dup_list = dups.get("list", [])
        if dup_list:
            print("DUPLICANTS:")
            print(f" {'Name':<15} | {'Health':<8} | {'Stress':<8} | {'Calories':<12} | {'Active Effect':<20}")
            print(" " + "-" * 78)
            for dup in dup_list:
                name = dup.get("name", "Unknown")
                health = dup.get("health", 0.0)
                stress = dup.get("stress", 0.0)
                calories = dup.get("calories", 0.0) / 1000.0 # kcal
                effect = dup.get("activeEffect", "None")
                print(f" {name:<15} | {health:>6.1f}% | {stress:>6.1f}% | {calories:>8.0f} kcal | {effect:<20}")
            print("-" * 80)

        # 3. Food Storage Panel
        food_items = food.get("items", [])
        if food_items:
            print("STORED FOODS:")
            print(f" {'Food ID':<22} | {'Display Name':<20} | {'Calories':<12} | {'Quantity':<10}")
            print(" " + "-" * 78)
            for item in food_items:
                fid = item.get("id", "Unknown")
                name = item.get("displayName", "Unknown")
                kcal = item.get("calories", 0.0) / 1000.0
                mass = item.get("amountKg", 0.0)
                print(f" {fid:<22} | {name:<20} | {kcal:>8.0f} kcal | {mass:>6.1f} kg")
            print("-" * 80)

        # 4. Critters Panel
        critters = summary.get("critters", [])
        active_critters = [c for c in critters if c.get("wildCount", 0) > 0 or c.get("domesticHappyCount", 0) > 0 or c.get("domesticGlumCount", 0) > 0 or c.get("eggsCount", 0) > 0]
        if active_critters:
            print("CRITTER RANCHES:")
            print(f" {'Species':<18} | {'Wild':<6} | {'Tame (Happy)':<13} | {'Tame (Glum)':<12} | {'Eggs':<6}")
            print(" " + "-" * 78)
            for c in active_critters:
                cid = c.get("id", "Unknown")
                wild = c.get("wildCount", 0)
                happy = c.get("domesticHappyCount", 0)
                glum = c.get("domesticGlumCount", 0)
                eggs = c.get("eggsCount", 0)
                print(f" {cid:<18} | {wild:<6} | {happy:<13} | {glum:<12} | {eggs:<6}")
            print("-" * 80)

        # 5. Crops Panel
        crops = summary.get("crops", [])
        active_crops = [c for c in crops if c.get("wildCount", 0) > 0 or c.get("domesticatedCount", 0) > 0]
        if active_crops:
            print("AGRICULTURE:")
            print(f" {'Crop':<25} | {'Wild':<6} | {'Domesticated':<13} | {'Farmer\'s Touch':<15} | {'Planters'}")
            print(" " + "-" * 78)
            for c in active_crops:
                cid = c.get("id", "Unknown")
                wild = c.get("wildCount", 0)
                domestic = c.get("domesticatedCount", 0)
                buff = c.get("farmersTouchCount", 0)
                planters = c.get("planterTypes", {})
                planters_str = ", ".join([f"{k}: {v}" for k, v in planters.items()])
                print(f" {cid:<25} | {wild:<6} | {domestic:<13} | {buff:<15} | {planters_str}")
            print("-" * 80)

        # 6. Stockpiled Resources Panel
        resources = summary.get("resources", {})
        if resources:
            print("STOCKPILED RESOURCES:")
            # Filter and display some key elements
            key_elements = ["Water", "DirtyWater", "Dirt", "Coal", "Phosphorite", "Algae", "Oxygen", "Slime", "SaltWater", "Sand"]
            row_items = []
            for elem in key_elements:
                amt = resources.get(elem, 0.0)
                if amt > 0:
                    row_items.append(f"{elem}: {amt/1000.0:,.1f} t")
            
            # Print in 3 columns
            for i in range(0, len(row_items), 3):
                chunk = row_items[i:i+3]
                line = " | ".join(f"{x:<24}" for x in chunk)
                print(" " + line)
            print("=" * 80)

def main():
    parser = argparse.ArgumentParser(description="ONILive Colony Telemetry Viewer")
    parser.add_argument("--summary", type=str, default=get_default_path("colony_summary.json"), help="Path to colony_summary.json")
    parser.add_argument("--history", type=str, default=get_default_path("colony_history.json"), help="Path to colony_history.json")
    args = parser.parse_args()

    viewer = ONILiveViewer(summary_path=args.summary, history_path=args.history)

    print("[*] Launching live colony companion viewer...")
    try:
        while True:
            viewer.render()
            time.sleep(2.0)
    except KeyboardInterrupt:
        print("\n[*] Exiting Live Colony Companion. Goodbye!")

if __name__ == "__main__":
    main()
