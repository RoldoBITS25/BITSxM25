using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MultiplayerGame.UI
{
    /// <summary>
    /// Debug helper to test button functionality
    /// Attach to any GameObject and check console for button click events
    /// </summary>
    public class ButtonDebugHelper : MonoBehaviour
    {
        [Header("Test Buttons")]
        [SerializeField] private Button testButton;

        private void Start()
        {
//             Debug.Log("[ButtonDebugHelper] Starting button debug helper");

            // Find all buttons in scene
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
//             Debug.Log($"[ButtonDebugHelper] Found {allButtons.Length} buttons in scene");

            foreach (Button btn in allButtons)
            {
                string buttonName = GetButtonPath(btn.transform);
//                 Debug.Log($"[ButtonDebugHelper] Button: {buttonName}");
                
                // Add debug listener to each button
                btn.onClick.AddListener(() => OnButtonClicked(buttonName));
            }

            // Check for EventSystem
            UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
//                 Debug.LogError("[ButtonDebugHelper] ✗ NO EVENTSYSTEM FOUND! Buttons won't work without it!");
            }
            else
            {
//                 Debug.Log($"[ButtonDebugHelper] ✓ EventSystem found: {eventSystem.name}");
            }

            // Check for GraphicRaycaster on Canvas
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (raycaster == null)
                {
//                     Debug.LogWarning($"[ButtonDebugHelper] ✗ Canvas '{canvas.name}' has no GraphicRaycaster! Buttons won't work!");
                }
                else
                {
//                     Debug.Log($"[ButtonDebugHelper] ✓ Canvas '{canvas.name}' has GraphicRaycaster");
                }
            }
        }

        private void OnButtonClicked(string buttonName)
        {
//             Debug.Log($"[ButtonDebugHelper] ★★★ BUTTON CLICKED: {buttonName} ★★★");
        }

        private string GetButtonPath(Transform transform)
        {
            string path = transform.name;
            Transform current = transform.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }

        [ContextMenu("List All Buttons")]
        public void ListAllButtons()
        {
            Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
//             Debug.Log($"========== Found {allButtons.Length} buttons ==========");
            
            foreach (Button btn in allButtons)
            {
                string buttonName = GetButtonPath(btn.transform);
//                 Debug.Log($"  - {buttonName}");
//                 Debug.Log($"    Interactable: {btn.interactable}");
//                 Debug.Log($"    Listeners: {btn.onClick.GetPersistentEventCount()}");
            }
        }

        [ContextMenu("Check UI Setup")]
        public void CheckUISetup()
        {
//             Debug.Log("========== UI Setup Check ==========");

            // Check EventSystem
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
//             Debug.Log($"EventSystem: {(eventSystem != null ? "✓ Found" : "✗ MISSING")}");

            // Check Canvases
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
//             Debug.Log($"Canvases: {canvases.Length}");
            
            foreach (Canvas canvas in canvases)
            {
//                 Debug.Log($"  Canvas: {canvas.name}");
//                 Debug.Log($"    Render Mode: {canvas.renderMode}");
//                 Debug.Log($"    GraphicRaycaster: {(canvas.GetComponent<GraphicRaycaster>() != null ? "✓" : "✗")}");
            }

            // Check RoomUI
            var roomUI = FindFirstObjectByType<RoomUI>();
            if (roomUI != null)
            {
//                 Debug.Log("RoomUI: ✓ Found");
            }
            else
            {
//                 Debug.Log("RoomUI: ✗ NOT FOUND");
            }

            // Check NetworkManager
            if (NetworkManager.Instance != null)
            {
//                 Debug.Log("NetworkManager: ✓ Found");
            }
            else
            {
//                 Debug.Log("NetworkManager: ✗ NOT FOUND");
            }
        }
    }
}
