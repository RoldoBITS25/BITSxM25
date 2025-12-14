using UnityEngine;
using UnityEngine.EventSystems;

namespace MultiplayerGame.UI
{
    /// <summary>
    /// Fixes EventSystem to work with new Input System
    /// Run this once to fix the Input System error
    /// </summary>
    public class FixEventSystem : MonoBehaviour
    {
        [ContextMenu("Fix EventSystem for New Input System")]
        public void FixInputSystem()
        {
//             Debug.Log("[FixEventSystem] Fixing EventSystem...");

            // Find EventSystem
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            
            if (eventSystem == null)
            {
//                 Debug.LogError("[FixEventSystem] No EventSystem found in scene!");
                return;
            }

//             Debug.Log($"[FixEventSystem] Found EventSystem: {eventSystem.name}");

            // Remove old StandaloneInputModule
            var oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
//                 Debug.Log("[FixEventSystem] Removing StandaloneInputModule...");
                DestroyImmediate(oldModule);
            }

            // Check if InputSystemUIInputModule already exists
            #if ENABLE_INPUT_SYSTEM
            var newModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (newModule == null)
            {
//                 Debug.Log("[FixEventSystem] Adding InputSystemUIInputModule...");
                eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
//                 Debug.Log("[FixEventSystem] ✓ Fixed! EventSystem now uses InputSystemUIInputModule");
            }
            else
            {
//                 Debug.Log("[FixEventSystem] ✓ InputSystemUIInputModule already exists");
            }
            #endif
        }

        private void Start()
        {
            // Auto-fix on start
            FixInputSystem();
            
            // Destroy this component after fixing
            Destroy(this);
        }
    }
}
