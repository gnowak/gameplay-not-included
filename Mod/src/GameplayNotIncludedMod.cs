using HarmonyLib;
using KMod;
using System;

namespace GameplayNotIncluded
{
    public class GameplayNotIncludedMod : UserMod2
    {
        public static WebSocketServer Server { get; private set; }

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            Debug.Log("[GameplayNotIncluded] GameplayNotIncludedMod loaded successfully.");

            try
            {
                foreach (var field in typeof(PrioritizeTool).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static))
                {
                    Debug.Log($"[GameplayNotIncluded] PrioritizeTool Field: {field.FieldType.FullName} {field.Name}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[GameplayNotIncluded] Failed to reflect PrioritizeTool: " + ex.Message);
            }

            StartServer();
        }

        private static void StartServer()
        {
            try
            {
                if (Server != null)
                {
                    Debug.Log("[GameplayNotIncluded] WebSocket server already running.");
                    return;
                }

                Server = new WebSocketServer();
                Server.Start(8080);
                Debug.Log("[GameplayNotIncluded] WebSocket server started successfully on ws://localhost:8080");
            }
            catch (Exception ex)
            {
                Debug.LogError("[GameplayNotIncluded] Failed to start WebSocket server: " + ex.ToString());
                Server = null;
            }
        }
    }
}
