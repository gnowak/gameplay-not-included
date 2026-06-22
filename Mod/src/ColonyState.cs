using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Klei.AI;

namespace GameplayNotIncluded
{
    [Serializable]
    public class DuplicantVitals
    {
        public string name;
        public float stress;
        public float calories;
        public float health;
        public string activeEffect;
        public List<string> activeEffects;
    }

    [Serializable]
    public class DuplicantsSection
    {
        public int count;
        public float averageStress;
        public List<DuplicantVitals> list;
    }

    [Serializable]
    public class FoodItemEntry
    {
        public string id;
        public string displayName;
        public float calories;
        public float amountKg;
    }

    [Serializable]
    public class FoodStorageSection
    {
        public float totalCalories;
        public List<FoodItemEntry> items;
    }

    [Serializable]
    public class CritterEntry
    {
        public string id;
        public int wild;
        public int domesticHappy;
        public int domesticGlum;
        public int eggs;
        public string activeFeed;

        // Legacy fields for backward compatibility
        public int wildCount;
        public int domesticHappyCount;
        public int domesticGlumCount;
        public int eggsCount;
    }

    [Serializable]
    public class CropEntry
    {
        public string id;
        public int wild;
        public int domesticated;
        public int farmersTouch;
        public Dictionary<string, int> planterTypes;

        // Legacy fields for backward compatibility
        public int wildCount;
        public int domesticatedCount;
        public int farmersTouchCount;
    }

    [Serializable]
    public class ColonySummary
    {
        public string colonyName;
        public int cycle;
        public DuplicantsSection duplicants;
        public FoodStorageSection foodStorage;
        public List<CritterEntry> critters;
        public List<CropEntry> crops;
        public Dictionary<string, float> resources;
    }

    [Serializable]
    public class ColonyHistoryEntry
    {
        public int cycle;
        public int duplicants;
        public float calories;
        public float averageStress;
        public float powerGenerated;
    }

    public static class ColonyState
    {
        private static List<EffectInstance> GetEffectsList(Effects effectsComponent)
        {
            try
            {
                var field = typeof(Effects).GetField("effects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(effectsComponent) as List<EffectInstance>;
                }
            }
            catch {}
            return null;
        }

        public static void WriteState()
        {
            try
            {
                if (GameClock.Instance == null || ClusterManager.Instance == null)
                {
                    Debug.Log("[GameplayNotIncluded] GameClock or ClusterManager is null. Skipping state export.");
                    return;
                }

                int activeWorldId = ClusterManager.Instance.activeWorldId;
                var activeWorld = ClusterManager.Instance.activeWorld;
                if (activeWorld == null)
                {
                    Debug.Log("[GameplayNotIncluded] Active world is null. Skipping state export.");
                    return;
                }

                int currentCycle = GameClock.Instance.GetCycle();

                // Build ColonySummary
                ColonySummary summary = new ColonySummary();
                summary.colonyName = SaveGame.Instance != null ? SaveGame.Instance.BaseName : "Unknown Asteroid";
                summary.cycle = currentCycle;

                // 1. Duplicants Section
                summary.duplicants = new DuplicantsSection();
                summary.duplicants.list = new List<DuplicantVitals>();
                
                float totalStress = 0f;
                int duplicantCount = 0;

                if (Components.LiveMinionIdentities != null && Components.LiveMinionIdentities.Items != null)
                {
                    foreach (var minion in Components.LiveMinionIdentities.Items)
                    {
                        if (minion != null && minion.GetMyWorldId() == activeWorldId)
                        {
                            duplicantCount++;
                            DuplicantVitals vitals = new DuplicantVitals();
                            vitals.name = minion.name;

                            // HP
                            vitals.health = 100f;
                            var hpAmt = Db.Get().Amounts.HitPoints.Lookup(minion.gameObject);
                            if (hpAmt != null) vitals.health = hpAmt.value;

                            // Stress
                            vitals.stress = 0f;
                            var stressAmount = Db.Get().Amounts.Stress.Lookup(minion.gameObject);
                            if (stressAmount != null)
                            {
                                vitals.stress = stressAmount.value;
                                totalStress += stressAmount.value;
                            }

                            // Calories
                            vitals.calories = 0f;
                            var calAmt = Db.Get().Amounts.Calories.Lookup(minion.gameObject);
                            if (calAmt != null) vitals.calories = calAmt.value;

                            // Active effects
                            vitals.activeEffect = "None";
                            vitals.activeEffects = new List<string>();
                            var effects = minion.GetComponent<Effects>();
                            if (effects != null)
                            {
                                var activeList = GetEffectsList(effects);
                                if (activeList != null && activeList.Count > 0)
                                {
                                    vitals.activeEffect = activeList[0].effect.Id;
                                    foreach (var activeEff in activeList)
                                    {
                                        if (activeEff?.effect != null)
                                        {
                                            vitals.activeEffects.Add(activeEff.effect.Id);
                                        }
                                    }
                                }
                            }
                            if (vitals.activeEffects.Count == 0)
                            {
                                vitals.activeEffects.Add("None");
                            }

                            summary.duplicants.list.Add(vitals);
                        }
                    }
                }
                summary.duplicants.count = duplicantCount;
                summary.duplicants.averageStress = duplicantCount > 0 ? (totalStress / duplicantCount) : 0f;

                // 2. Food Storage Section
                summary.foodStorage = new FoodStorageSection();
                summary.foodStorage.items = new List<FoodItemEntry>();
                
                float totalKcals = 0f;
                var foodItemsMap = new Dictionary<string, FoodItemEntry>();

                if (Components.Edibles != null && Components.Edibles.Items != null)
                {
                    foreach (var edible in Components.Edibles.Items)
                    {
                        if (edible != null && edible.GetMyWorldId() == activeWorldId)
                        {
                            totalKcals += edible.Calories;

                            var prefabID = edible.GetComponent<KPrefabID>();
                            if (prefabID == null) continue;

                            string foodId = prefabID.PrefabTag.Name;
                            string displayName = edible.GetProperName();
                            float calories = edible.Calories;

                            var primaryElement = edible.GetComponent<PrimaryElement>();
                            float mass = primaryElement != null ? primaryElement.Mass : 0f;

                            if (foodItemsMap.ContainsKey(foodId))
                            {
                                foodItemsMap[foodId].calories += calories;
                                foodItemsMap[foodId].amountKg += mass;
                            }
                            else
                            {
                                foodItemsMap[foodId] = new FoodItemEntry
                                {
                                    id = foodId,
                                    displayName = displayName,
                                    calories = calories,
                                    amountKg = mass
                                };
                            }
                        }
                    }
                }
                summary.foodStorage.totalCalories = totalKcals;
                summary.foodStorage.items.AddRange(foodItemsMap.Values);

                // 3. Critters Section
                var crittersMap = new Dictionary<string, CritterEntry>();
                string[] baseCritterIds = { "Hatch", "Drecko", "LightBug", "Squirrel", "Pacu", "DivergentBeetle", "IceBelly", "Squid" };
                foreach (var id in baseCritterIds)
                {
                    crittersMap[id] = new CritterEntry
                    {
                        id = id,
                        wild = 0,
                        domesticHappy = 0,
                        domesticGlum = 0,
                        eggs = 0,
                        activeFeed = null,

                        wildCount = 0,
                        domesticHappyCount = 0,
                        domesticGlumCount = 0,
                        eggsCount = 0
                    };
                }

                // Count live critters using Components.Brains
                if (Components.Brains != null && Components.Brains.Items != null)
                {
                    foreach (var brain in Components.Brains.Items)
                    {
                        if (brain != null && brain is CreatureBrain && brain.GetMyWorldId() == activeWorldId)
                        {
                            var critter = brain.gameObject;
                            var prefabID = critter.GetComponent<KPrefabID>();
                            if (prefabID == null) continue;

                            string mappedId = GetMappedCritterId(prefabID.PrefabTag.Name);
                            if (mappedId == null || !crittersMap.ContainsKey(mappedId)) continue;

                            var traits = critter.GetComponent<Traits>();
                            bool isWild = traits != null && traits.HasTrait("Wild");

                            if (isWild)
                            {
                                crittersMap[mappedId].wild++;
                                crittersMap[mappedId].wildCount++;
                            }
                            else
                            {
                                var effects = critter.GetComponent<Effects>();
                                bool isGlum = effects != null && effects.HasEffect("Glum");

                                if (isGlum)
                                {
                                    crittersMap[mappedId].domesticGlum++;
                                    crittersMap[mappedId].domesticGlumCount++;
                                }
                                else
                                {
                                    crittersMap[mappedId].domesticHappy++;
                                    crittersMap[mappedId].domesticHappyCount++;
                                }
                            }
                        }
                    }
                }

                // Count eggs
                if (Components.Pickupables != null && Components.Pickupables.Items != null)
                {
                    foreach (var pickupable in Components.Pickupables.Items)
                    {
                        if (pickupable != null && pickupable.GetMyWorldId() == activeWorldId)
                        {
                            var prefabID = pickupable.GetComponent<KPrefabID>();
                            if (prefabID == null) continue;

                            string mappedId = GetMappedEggId(prefabID.PrefabTag.Name);
                            if (mappedId != null && crittersMap.ContainsKey(mappedId))
                            {
                                crittersMap[mappedId].eggs++;
                                crittersMap[mappedId].eggsCount++;
                            }
                        }
                    }
                }

                summary.critters = new List<CritterEntry>(crittersMap.Values);

                // 4. Crops Section
                var cropsMap = new Dictionary<string, CropEntry>();
                string[] baseCropIds = {
                    "BasicSingleHarvestPlant", "PrickleFlower", "MushroomPlant", "ColdWheat",
                    "BasicFabricPlant", "SpiceVine", "SeaLettuce", "SuperWormPlant"
                };
                foreach (var id in baseCropIds)
                {
                    cropsMap[id] = new CropEntry
                    {
                        id = id,
                        wild = 0,
                        domesticated = 0,
                        farmersTouch = 0,
                        planterTypes = new Dictionary<string, int>(),

                        wildCount = 0,
                        domesticatedCount = 0,
                        farmersTouchCount = 0
                    };
                }

                if (Components.Uprootables != null && Components.Uprootables.Items != null)
                {
                    foreach (var uprootable in Components.Uprootables.Items)
                    {
                        if (uprootable != null && uprootable.GetMyWorldId() == activeWorldId)
                        {
                            var prefabID = uprootable.GetComponent<KPrefabID>();
                            if (prefabID == null) continue;

                            string mappedId = GetMappedCropId(prefabID.PrefabTag.Name);
                            if (mappedId == null || !cropsMap.ContainsKey(mappedId)) continue;

                            var rm = uprootable.GetComponent<ReceptacleMonitor>();
                            bool isDomestic = rm != null && rm.Replanted;

                            if (isDomestic)
                            {
                                cropsMap[mappedId].domesticated++;
                                cropsMap[mappedId].domesticatedCount++;

                                var plot = rm.GetReceptacle();
                                string planterName = plot != null ? plot.name.Replace("(Clone)", "") : "UnknownPlanter";
                                if (cropsMap[mappedId].planterTypes.ContainsKey(planterName))
                                {
                                    cropsMap[mappedId].planterTypes[planterName]++;
                                }
                                else
                                {
                                    cropsMap[mappedId].planterTypes[planterName] = 1;
                                }

                                var effects = uprootable.GetComponent<Effects>();
                                if (effects != null && effects.HasEffect("FarmersTouch"))
                                {
                                    cropsMap[mappedId].farmersTouch++;
                                    cropsMap[mappedId].farmersTouchCount++;
                                }
                            }
                            else
                            {
                                cropsMap[mappedId].wild++;
                                cropsMap[mappedId].wildCount++;
                            }
                        }
                    }
                }

                summary.crops = new List<CropEntry>(cropsMap.Values);

                // 5. Resources Section
                summary.resources = new Dictionary<string, float>();
                if (activeWorld.worldInventory != null && ElementLoader.elements != null)
                {
                    foreach (var element in ElementLoader.elements)
                    {
                        if (element != null)
                        {
                            float amount = activeWorld.worldInventory.GetAmount(element.tag, false);
                            if (amount > 0f)
                            {
                                summary.resources[element.tag.Name] = amount;
                            }
                        }
                    }
                }

                // Write colony_summary.json
                string dirPath = GetOutputDir();
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                 string summaryPath = Path.Combine(dirPath, "colony_summary.json");
                string summaryJson = JsonConvert.SerializeObject(summary, Formatting.Indented);
                File.WriteAllText(summaryPath, summaryJson);
                Debug.Log("[GameplayNotIncluded] Exported colony summary to " + summaryPath);

                // Broadcast to WebSocket clients on the background thread
                if (GameplayNotIncludedMod.Server != null)
                {
                    GameplayNotIncludedMod.Server.EnqueueMessage(summaryJson);
                }

                // 6. Write/Update colony_history.json
                UpdateHistory(dirPath, currentCycle, duplicantCount, totalKcals, summary.duplicants.averageStress);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameplayNotIncluded] Error exporting colony state: " + ex.ToString());
            }
        }

        private static string GetMappedCritterId(string prefabName)
        {
            if (prefabName.Contains("Hatch")) return "Hatch";
            if (prefabName.Contains("Drecko")) return "Drecko";
            if (prefabName.Contains("LightBug")) return "LightBug";
            if (prefabName.Contains("Squirrel")) return "Squirrel";
            if (prefabName.Contains("Pacu")) return "Pacu";
            if (prefabName.Contains("DivergentBeetle")) return "DivergentBeetle";
            if (prefabName.Contains("IceBelly") || prefabName.Contains("Belly")) return "IceBelly";
            if (prefabName.Contains("Squid")) return "Squid";
            return null;
        }

        private static string GetMappedEggId(string prefabName)
        {
            if (prefabName.Contains("HatchEgg")) return "Hatch";
            if (prefabName.Contains("DreckoEgg")) return "Drecko";
            if (prefabName.Contains("LightBugEgg")) return "LightBug";
            if (prefabName.Contains("SquirrelEgg")) return "Squirrel";
            if (prefabName.Contains("PacuEgg")) return "Pacu";
            if (prefabName.Contains("DivergentBeetleEgg")) return "DivergentBeetle";
            if (prefabName.Contains("IceBellyEgg") || prefabName.Contains("BellyEgg")) return "IceBelly";
            if (prefabName.Contains("SquidEgg")) return "Squid";
            return null;
        }

        private static string GetMappedCropId(string prefabName)
        {
            if (prefabName == "BasicSingleHarvestPlant") return "BasicSingleHarvestPlant";
            if (prefabName == "PrickleFlower") return "PrickleFlower";
            if (prefabName == "MushroomPlant") return "MushroomPlant";
            if (prefabName == "ColdWheat") return "ColdWheat";
            if (prefabName == "BasicFabricPlant") return "BasicFabricPlant";
            if (prefabName == "SpiceVine") return "SpiceVine";
            if (prefabName == "SeaLettuce") return "SeaLettuce";
            if (prefabName == "SuperWormPlant") return "SuperWormPlant";
            return null;
        }

        private static void UpdateHistory(string dirPath, int currentCycle, int duplicants, float calories, float averageStress)
        {
            try
            {
                string historyPath = Path.Combine(dirPath, "colony_history.json");
                List<ColonyHistoryEntry> history = new List<ColonyHistoryEntry>();

                if (File.Exists(historyPath))
                {
                    try
                    {
                        string existingJson = File.ReadAllText(historyPath);
                        if (!string.IsNullOrEmpty(existingJson))
                        {
                            var parsedHistory = JsonConvert.DeserializeObject<List<ColonyHistoryEntry>>(existingJson);
                            if (parsedHistory != null)
                            {
                                history = parsedHistory;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[GameplayNotIncluded] Failed to read existing history. Starting fresh: " + ex.Message);
                    }
                }

                // Truncate any cycles >= currentCycle to handle save reloads cleanly
                history.RemoveAll(entry => entry.cycle >= currentCycle);

                // Fetch power generated
                float powerGenerated = GetPowerGenerated();

                // Append current cycle's entry
                history.Add(new ColonyHistoryEntry
                {
                    cycle = currentCycle,
                    duplicants = duplicants,
                    calories = calories,
                    averageStress = averageStress,
                    powerGenerated = powerGenerated
                });

                string historyJson = JsonConvert.SerializeObject(history, Formatting.Indented);
                File.WriteAllText(historyPath, historyJson);
                Debug.Log("[GameplayNotIncluded] Exported colony history to " + historyPath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameplayNotIncluded] Error updating colony history: " + ex.ToString());
            }
        }

        private static float GetPowerGenerated()
        {
            try
            {
                if (ReportManager.Instance != null && ReportManager.Instance.TodaysReport != null)
                {
                    var entry = ReportManager.Instance.TodaysReport.GetEntry(ReportManager.ReportType.EnergyCreated);
                    if (entry != null)
                    {
                        return entry.Positive;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameplayNotIncluded] Failed to read power report entry: " + ex.Message);
            }
            return 0f;
        }

        private static string GetOutputDir()
        {
            try
            {
                string docsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                string kleiPath = Path.Combine(docsPath, "Klei", "OxygenNotIncluded", "gameplay-not-included");
                return kleiPath;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GameplayNotIncluded] Failed to get MyDocuments path, falling back to C:\\ONI_Agent: " + ex.Message);
                return @"C:\ONI_Agent";
            }
        }
    }
}
