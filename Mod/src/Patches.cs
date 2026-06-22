using UnityEngine;
using HarmonyLib;

namespace GameplayNotIncluded
{
    public class GameplayNotIncludedController : MonoBehaviour
    {
        private float timeSinceLastStateDump = 0f;
        private float stateDumpInterval = 10f; // Dump state every 10 seconds

        private void Start()
        {
            Debug.Log("[GameplayNotIncluded] GameplayNotIncludedController started.");
            ColonyState.WriteState();
        }

        private void Update()
        {
            timeSinceLastStateDump += Time.deltaTime;
            if (timeSinceLastStateDump >= stateDumpInterval)
            {
                timeSinceLastStateDump = 0f;
                ColonyState.WriteState();
            }
        }
    }

    [HarmonyPatch(typeof(Game), "OnSpawn")]
    public static class Game_OnSpawn_Patch
    {
        public static void Postfix()
        {
            if (GameClock.Instance != null)
            {
                GameClock.Instance.Subscribe((int)GameHashes.NewDay, OnNewDay);
                Debug.Log("[GameplayNotIncluded] Subscribed to GameHashes.NewDay successfully on Game spawn.");
            }
            else
            {
                Debug.LogWarning("[GameplayNotIncluded] GameClock.Instance is null during Game spawn.");
            }

            try
            {
                if (Game.Instance != null)
                {
                    GameplayNotIncludedController controller = Game.Instance.gameObject.GetComponent<GameplayNotIncludedController>();
                    if (controller == null)
                    {
                        Game.Instance.gameObject.AddComponent<GameplayNotIncludedController>();
                        Debug.Log("[GameplayNotIncluded] Successfully added GameplayNotIncludedController component to Game.Instance.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[GameplayNotIncluded] Failed to add GameplayNotIncludedController: " + ex.Message);
            }
        }

        private static void OnNewDay(object data)
        {
            Debug.Log("[GameplayNotIncluded] GameClock detected a NewDay event! Writing colony state.");
            ColonyState.WriteState();
        }
    }
}
