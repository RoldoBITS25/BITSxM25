using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerGame.UI
{
    /// <summary>
    /// Manages the main menu flow and coordinates between UI and game scene setup
    /// Ensures the menu is shown first, then triggers scene setup after room is joined
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        public static MainMenuManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private RoomUI roomUI;
        [SerializeField] private GameObject menuCanvas;

        [Header("Scene Setup")]
        [SerializeField] private bool disableSceneSetupUntilRoomJoined = true;
        [SerializeField] private MainSceneSetup sceneSetup;

        private bool hasJoinedRoom = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
//                 Debug.Log("[MainMenuManager] Initialized");
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Find references if not set
            if (roomUI == null)
                roomUI = FindFirstObjectByType<RoomUI>();

            if (sceneSetup == null)
                sceneSetup = FindFirstObjectByType<MainSceneSetup>();
        }

        private void Start()
        {
            // Subscribe to network events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomCreated += OnRoomJoinedOrCreated;
                NetworkManager.Instance.OnRoomJoined += OnRoomJoinedOrCreated;
                NetworkManager.Instance.OnRoomLeft += OnRoomLeft;
            }

            // Show menu canvas
            if (menuCanvas != null)
            {
                menuCanvas.SetActive(true);
//                 Debug.Log("[MainMenuManager] Menu canvas shown");
            }

            // Disable scene setup if configured
            if (disableSceneSetupUntilRoomJoined && sceneSetup != null)
            {
                // Disable auto setup
                var sceneSetupType = typeof(MainSceneSetup);
                var autoSetupField = sceneSetupType.GetField("autoSetupOnStart",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (autoSetupField != null)
                {
                    autoSetupField.SetValue(sceneSetup, false);
//                     Debug.Log("[MainMenuManager] Disabled auto scene setup");
                }
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomCreated -= OnRoomJoinedOrCreated;
                NetworkManager.Instance.OnRoomJoined -= OnRoomJoinedOrCreated;
                NetworkManager.Instance.OnRoomLeft -= OnRoomLeft;
            }
        }

        private void OnRoomJoinedOrCreated(Room room)
        {
//             Debug.Log($"[MainMenuManager] Room joined/created: {room.name}");
            hasJoinedRoom = true;

            // Hide menu canvas (lobby panel will still be visible via RoomUI)
            // We keep the canvas active but RoomUI will manage panel visibility
            
            // Trigger scene setup now that we're in a room
            if (sceneSetup != null && disableSceneSetupUntilRoomJoined)
            {
//                 Debug.Log("[MainMenuManager] Triggering scene setup");
                sceneSetup.SetupScene();
            }
        }

        private void OnRoomLeft()
        {
//             Debug.Log("[MainMenuManager] Room left, returning to menu");
            hasJoinedRoom = false;

            // Show menu canvas again
            if (menuCanvas != null)
            {
                menuCanvas.SetActive(true);
            }
        }

        /// <summary>
        /// Check if player is currently in a room
        /// </summary>
        public bool IsInRoom()
        {
            return hasJoinedRoom;
        }

        /// <summary>
        /// Manually trigger scene setup (for testing)
        /// </summary>
        [ContextMenu("Force Scene Setup")]
        public void ForceSceneSetup()
        {
            if (sceneSetup != null)
            {
                sceneSetup.SetupScene();
            }
        }
    }
}
