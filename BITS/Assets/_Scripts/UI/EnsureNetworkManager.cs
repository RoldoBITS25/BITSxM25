using UnityEngine;

namespace MultiplayerGame.UI
{
    /// <summary>
    /// Simple helper to ensure NetworkManager exists in the scene
    /// Attach this to any GameObject or it will create one automatically
    /// </summary>
    public class EnsureNetworkManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string serverUrl = "http://10.166.9.144:8000";
        [SerializeField] private string wsUrl = "ws://10.166.9.144:8000";

        private void Awake()
        {
            // Check if NetworkManager already exists
            if (NetworkManager.Instance != null)
            {
                Debug.Log("[EnsureNetworkManager] NetworkManager already exists");
                return;
            }

            // Create NetworkManager
            Debug.Log("[EnsureNetworkManager] Creating NetworkManager...");
            GameObject networkManagerObj = new GameObject("NetworkManager");
            NetworkManager netManager = networkManagerObj.AddComponent<NetworkManager>();

            // Set server URLs using reflection (they're private serialized fields)
            var netManagerType = typeof(NetworkManager);
            var serverUrlField = netManagerType.GetField("serverUrl",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var wsUrlField = netManagerType.GetField("wsUrl",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (serverUrlField != null)
                serverUrlField.SetValue(netManager, serverUrl);
            if (wsUrlField != null)
                wsUrlField.SetValue(netManager, wsUrl);

            Debug.Log($"[EnsureNetworkManager] ✓ NetworkManager created with server: {serverUrl}");
        }
    }
}
