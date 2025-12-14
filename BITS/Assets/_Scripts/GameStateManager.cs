using System; // For Enum parsing
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Manages the game state and synchronizes with the server
    /// Handles spawning, updating, and destroying game objects
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject spectatorCameraPrefab;
        [SerializeField] private Dictionary<string, GameObject> objectPrefabs = new Dictionary<string, GameObject>();

        [Header("Spawn Points")]
        [SerializeField] private Transform player1SpawnPoint;
        [SerializeField] private Transform player2SpawnPoint;

        private Dictionary<string, GameObject> spawnedPlayers = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();
        private GameObject localPlayerObject;
        private GameObject spectatorCamera;

        private void Awake()
        {
            Debug.Log("[GameStateManager] ========== GameStateManager Initializing ==========");
            
            if (Instance == null)
            {
                Instance = this;
                Debug.Log("[GameStateManager] ✓ Initialized as singleton");
            }
            else
            {
                Debug.LogWarning("[GameStateManager] Duplicate instance detected, destroying");
                Destroy(gameObject);
                return;
            }
            
            // Ensure we have a player prefab
            if (playerPrefab == null)
            {
                Debug.LogWarning("[GameStateManager] No player prefab assigned, creating default prefab");
                CreateDefaultPlayerPrefab();
            }
            else
            {
                Debug.Log($"[GameStateManager] ✓ Player prefab assigned: {playerPrefab.name}");
            }
            
            // Ensure spawn points exist
            EnsureSpawnPointsExist();
            
            Debug.Log("[GameStateManager] ========== GameStateManager Ready ==========");

            // Check if we are already in a room (e.g., scene loaded after joining)
            if (NetworkManager.Instance != null && NetworkManager.Instance.CurrentRoom != null)
            {
                Debug.Log("[GameStateManager] Already in room, triggering join logic manually");
                OnRoomJoined(NetworkManager.Instance.CurrentRoom);
            }
        }

        private void Start()
        {
            // Subscribe to network events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomJoined += OnRoomJoined;
                NetworkManager.Instance.OnRoomLeft += OnRoomLeft;
                NetworkManager.Instance.OnPlayerActionReceived += OnPlayerActionReceived;
                NetworkManager.Instance.OnGameStateUpdated += OnGameStateUpdated;
            }
        }

        private void OnRoomJoined(Room room)
        {
            // Spawn local player or spectator camera
            if (NetworkManager.Instance.IsPlayer)
            {
                SpawnLocalPlayer();
            }
            else
            {
                SpawnSpectatorCamera();
            }

            // Spawn other players
            for (int i = 0; i < room.current_players.Count; i++)
            {
                string playerId = room.current_players[i];
                // Player numbers are 1-based (index + 1)
                if (playerId != NetworkManager.Instance.PlayerId && !spawnedPlayers.ContainsKey(playerId))
                {
                    SpawnRemotePlayer(playerId, i + 1);
                }
            }
        }

        private void OnRoomLeft()
        {
            // Clean up all spawned objects
            foreach (var player in spawnedPlayers.Values)
            {
                Destroy(player);
            }
            spawnedPlayers.Clear();

            foreach (var obj in spawnedObjects.Values)
            {
                Destroy(obj);
            }
            spawnedObjects.Clear();

            if (localPlayerObject != null)
            {
                Destroy(localPlayerObject);
                localPlayerObject = null;
            }

            if (spectatorCamera != null)
            {
                Destroy(spectatorCamera);
                spectatorCamera = null;
            }
        }

        private void SpawnLocalPlayer()
        {
            if (localPlayerObject != null) return;

            Debug.Log($"[GameStateManager] ========== Spawning Local Player ==========");
            Debug.Log($"[GameStateManager] Player ID: {NetworkManager.Instance.PlayerId}");
            Debug.Log($"[GameStateManager] Player Number: {NetworkManager.Instance.PlayerNumber}");
            
            if (playerPrefab == null)
            {
                Debug.LogError("[GameStateManager] ✗ Player prefab not assigned! Cannot spawn player.");
                Debug.LogError("[GameStateManager] This should have been created in Awake(). Check for errors above.");
                return;
            }

            Transform spawnPoint = NetworkManager.Instance.PlayerNumber == 1 ? player1SpawnPoint : player2SpawnPoint;
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            
            Debug.Log($"[GameStateManager] Spawn point: {(spawnPoint != null ? spawnPoint.name : "null (using Vector3.zero)")}");
            Debug.Log($"[GameStateManager] Spawn position: {spawnPosition}");

            localPlayerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            localPlayerObject.name = $"LocalPlayer_{NetworkManager.Instance.PlayerId}";
            localPlayerObject.SetActive(true); // Ensure it's active!
            
            Debug.Log($"[GameStateManager] ✓ Player object instantiated: {localPlayerObject.name}");

            // Enable player controller
            var controller = localPlayerObject.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.Initialize(NetworkManager.Instance.PlayerId, true);
                Debug.Log($"[GameStateManager] ✓ PlayerController initialized");
            }
            else
            {
                Debug.LogWarning($"[GameStateManager] No PlayerController found on player prefab");
            }

            // Set player color based on player number
            SetPlayerColor(localPlayerObject, NetworkManager.Instance.PlayerNumber);
            
            // Set up camera follow
            var camera = Camera.main;
            if (camera != null)
            {
                var topDownCamera = camera.GetComponent<TopDownCamera>();
                if (topDownCamera == null)
                {
                    Debug.LogWarning("[GameStateManager] TopDownCamera component not found on Main Camera, adding it now");
                    topDownCamera = camera.gameObject.AddComponent<TopDownCamera>();
                }
                
                if (topDownCamera != null)
                {
                    topDownCamera.SetFollowTarget(localPlayerObject.transform);
                    Debug.Log($"[GameStateManager] ✓ Camera follow target set to local player");
                }
            }
            else
            {
                Debug.LogWarning("[GameStateManager] Main Camera not found for follow target setup");
            }
            
            Debug.Log($"[GameStateManager] ========== Local Player Spawned Successfully ==========");
            
            // Verify WebSocket connection is active
            if (NetworkManager.Instance != null)
            {
                Debug.Log($"[GameStateManager] Verifying WebSocket connection for movement replication...");
                NetworkManager.Instance.VerifyConnection();
            }
        }

        public void SpawnRemotePlayer(string playerId, int playerNumber)
        {
            // Delegate to the transform-based spawn, using spawn point if available
            Transform spawnPoint = playerNumber == 1 ? player1SpawnPoint : player2SpawnPoint;
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;

            SpawnRemotePlayer(playerId, spawnPosition, spawnRotation, playerNumber);
        }

        public void SpawnRemotePlayer(string playerId, Vector3 position, Quaternion rotation)
        {
            // Default player number 0 or logic to determine it
            // Ideally we'd know the player number to set the color correctly
            // For now, we'll default to 0 (spectator/neutral) or try to find a free slot visually if needed
            // But let's just use 3 for "other" as used elsewhere
            SpawnRemotePlayer(playerId, position, rotation, 3);
        }

        private void SpawnRemotePlayer(string playerId, Vector3 position, Quaternion rotation, int playerNumber)
        {
            if (playerPrefab == null) return;
            
            // acts as "SpawnOrUpdate" essentially if we check existence
            if (spawnedPlayers.TryGetValue(playerId, out GameObject existingPlayer))
            {
                // Update existing
                UpdatePlayerPosition(playerId, position, rotation);
                return;
            }

            GameObject remotePlayer = Instantiate(playerPrefab, position, rotation);
            remotePlayer.name = $"RemotePlayer_{playerId}";
            remotePlayer.SetActive(true); // Ensure it's active!

            // Disable local control for remote players
            var controller = remotePlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.Initialize(playerId, false);
            }

            // Add RemotePlayerController for smoothing
            var remoteController = remotePlayer.AddComponent<RemotePlayerController>();
            remoteController.Initialize(playerId);

            // Set color for remote player
            SetPlayerColor(remotePlayer, playerNumber);

            spawnedPlayers[playerId] = remotePlayer;
            Debug.Log($"[GameStateManager] Spawned remote player: {playerId} (Player {playerNumber}) at {position}");
        }

        private void SpawnSpectatorCamera()
        {
            if (spectatorCameraPrefab != null)
            {
                spectatorCamera = Instantiate(spectatorCameraPrefab);
                spectatorCamera.name = "SpectatorCamera";
            }
            else
            {
                Debug.LogWarning("Spectator camera prefab not assigned!");
            }
        }

        private void SetPlayerColor(GameObject player, int playerNumber)
        {
            var renderer = player.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Player 1 = Red, Player 2 = Green
                Color color = playerNumber == 1 ? Color.red : Color.green;
                renderer.material.color = color;
            }
        }

        private void OnPlayerActionReceived(PlayerAction action)
        {
            Debug.Log($"[GameStateManager] OnPlayerActionReceived: player={action.player_id}, action={action.action_type}, position={action.position}");
            
            // Handle actions from other players
            if (action.player_id == NetworkManager.Instance.PlayerId)
            {
                Debug.Log($"[GameStateManager] Ignoring own action");
                return; // Ignore our own actions
            }

            switch (action.action_type)
            {
                case "move":
                    UpdatePlayerPosition(action.player_id, action.position, action.rotation);
                    break;

                case "grab":
                    HandleGrabAction(action.player_id, action.target_object_id);
                    break;

                case "cut":
                    HandleCutAction(action.target_object_id, action.position);
                    break;

                case "break":
                    HandleBreakAction(action.target_object_id);
                    break;
                    
                case "swap_weapon":
                    HandleSwapWeaponAction(action.player_id, action.weapon);
                    break;
            }
        }

        private void HandleSwapWeaponAction(string playerId, string weaponStr)
        {
            if (string.IsNullOrEmpty(weaponStr)) return;
            
            if (Enum.TryParse(weaponStr, true, out WeaponType weapon))
            {
                if (spawnedPlayers.TryGetValue(playerId, out GameObject player))
                {
                    var controller = player.GetComponent<PlayerController>();
                    if (controller != null)
                    {
                        controller.CurrentWeapon = weapon;
                        Debug.Log($"[GameStateManager] Updated player {playerId} weapon to {weapon}");
                        
                        // TODO: Visual update
                    }
                }
            }
        }

        public bool IsWeaponTaken(WeaponType weapon)
        {
            if (weapon == WeaponType.None) return false;

            foreach (var kvp in spawnedPlayers)
            {
                // Skip local player if contained (spawnedPlayers usually contains remotes, but verify)
                if (kvp.Key == NetworkManager.Instance.PlayerId) continue;

                var pc = kvp.Value.GetComponent<PlayerController>();
                if (pc != null && pc.CurrentWeapon == weapon)
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdatePlayerPosition(string playerId, Vector3 position, Quaternion? rotation = null)
        {
            Debug.Log($"[GameStateManager] UpdatePlayerPosition: player={playerId}, position={position}, rotation={rotation}");
            
            if (spawnedPlayers.TryGetValue(playerId, out GameObject player))
            {
                Debug.Log($"[GameStateManager] Found spawned player {playerId}, updating position");
                // Use RemotePlayerController for smoothing if available
                var remoteController = player.GetComponent<RemotePlayerController>();
                if (remoteController != null)
                {
                    if (rotation.HasValue)
                        remoteController.SetTarget(position, rotation.Value);
                    else
                        remoteController.SetTarget(position);
                }
                else
                {
                    // Fallback to direct teleport
                    player.transform.position = position;
                    if (rotation.HasValue)
                        player.transform.rotation = rotation.Value;
                }
            }
            else
            {
                // Lazy spawn if we don't know this player
                Debug.Log($"[GameStateManager] Received action for unknown player {playerId}, spawning now.");
                SpawnRemotePlayer(playerId, 3); // Default to player 3 (spectator/other) color
                
                // Try applying update immediately after spawn
                if (spawnedPlayers.TryGetValue(playerId, out GameObject newPlayer))
                {
                    newPlayer.transform.position = position;
                    if (rotation.HasValue)
                        newPlayer.transform.rotation = rotation.Value;
                        
                    var rc = newPlayer.GetComponent<RemotePlayerController>();
                    if (rc != null) 
                    {
                        rc.Initialize(playerId); // Re-init to capture new pos
                    }
                }
            }
        }

        private void HandleGrabAction(string playerId, string objectId)
        {
            if (spawnedObjects.TryGetValue(objectId, out GameObject obj))
            {
                var interactable = obj.GetComponent<InteractableObject>();
                if (interactable != null)
                {
                    interactable.OnGrabbed(playerId);
                }
            }
        }

        private void HandleCutAction(string objectId, Vector3 cutPosition)
        {
            if (spawnedObjects.TryGetValue(objectId, out GameObject obj))
            {
                var interactable = obj.GetComponent<InteractableObject>();
                if (interactable != null)
                {
                    interactable.OnCut(cutPosition);
                }
            }
        }

        private void HandleBreakAction(string objectId)
        {
            if (spawnedObjects.TryGetValue(objectId, out GameObject obj))
            {
                var interactable = obj.GetComponent<InteractableObject>();
                if (interactable != null)
                {
                    interactable.OnBreak();
                }
                
                spawnedObjects.Remove(objectId);
            }
        }

        private void OnGameStateUpdated(GameState state)
        {
            // Sync all object states
            foreach (var objectState in state.objects)
            {
                if (spawnedObjects.TryGetValue(objectState.object_id, out GameObject obj))
                {
                    obj.transform.position = objectState.position;
                    obj.transform.rotation = objectState.rotation;
                }
            }

            // Sync player positions
            foreach (var playerState in state.players)
            {
                if (playerState.player_id != NetworkManager.Instance.PlayerId)
                {
                    UpdatePlayerPosition(playerState.player_id, playerState.position);
                }
            }
        }

        public void RegisterObject(string objectId, GameObject obj)
        {
            if (!spawnedObjects.ContainsKey(objectId))
            {
                spawnedObjects[objectId] = obj;
            }
        }

        public void UnregisterObject(string objectId)
        {
            spawnedObjects.Remove(objectId);
        }

        /// <summary>
        /// Creates a default player prefab if none is assigned
        /// This ensures players can spawn even without a manually created prefab
        /// </summary>
        private void CreateDefaultPlayerPrefab()
        {
            Debug.Log("[GameStateManager] Creating default player prefab...");
            
            // Create a simple empty object
            GameObject prefab = new GameObject("DefaultPlayerPrefab");
            
            // Add Rigidbody for 3D physics
            Rigidbody rb = prefab.AddComponent<Rigidbody>();
            if (rb != null)
            {
                rb.freezeRotation = true;
                rb.useGravity = false; // Top-down game, no gravity by default or handled by ground check
                rb.linearDamping = 5f; // Smooth movement
                
                // Constraints: freeze Y position to keep on ground plane (optional, but good for top-down)
                // rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
                // Currently just freezing rotation as per previous logic, but gravity is off
            }
            else
            {
                Debug.LogError("[GameStateManager] Critical Error: Could not add Rigidbody!");
            }
            
            // Add SphereCollider for 3D collision
            SphereCollider collider = prefab.AddComponent<SphereCollider>();
            if (collider != null)
                collider.radius = 0.5f;
            
            // Add a visual representation (Sphere for "Ball" look)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(prefab.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one; // 1x1x1 sphere
            
            // Remove the 3D collider from the visual child as we have one on the root
            if (visual.GetComponent<Collider>())
                Destroy(visual.GetComponent<Collider>());
            
            // Add PlayerController
            prefab.AddComponent<PlayerController>();
            
            // Add PlayerInput component for Input System
            prefab.AddComponent<UnityEngine.InputSystem.PlayerInput>();
            
            // Set default material color if material exists
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Unlit/Color")); // Use unlit shader for guaranteed visibility
                mat.color = Color.white; 
                renderer.material = mat;
            }
            
            // Don't activate it in the scene, just use as prefab reference
            prefab.SetActive(false);
            
            playerPrefab = prefab;
            
            Debug.Log("[GameStateManager] ✓ Default player prefab created (Sphere 3D)");
        }
        
        /// <summary>
        /// Ensures spawn points exist in the scene
        /// Creates them if they don't exist
        /// </summary>
        private void EnsureSpawnPointsExist()
        {
            Debug.Log("[GameStateManager] Checking spawn points...");
            
            // Find or create spawn points parent
            GameObject spawnPointsParent = GameObject.Find("SpawnPoints");
            if (spawnPointsParent == null)
            {
                spawnPointsParent = new GameObject("SpawnPoints");
                Debug.Log("[GameStateManager] Created SpawnPoints parent object");
            }
            
            // Player 1 spawn point
            if (player1SpawnPoint == null)
            {
                GameObject player1Spawn = GameObject.Find("Player1SpawnPoint");
                if (player1Spawn == null)
                {
                    player1Spawn = new GameObject("Player1SpawnPoint");
                    player1Spawn.transform.SetParent(spawnPointsParent.transform);
                    player1Spawn.transform.position = new Vector3(-10f, 0.5f, 0f);
                    Debug.Log("[GameStateManager] Created Player1SpawnPoint at (-10, 0.5, 0)");
                }
                player1SpawnPoint = player1Spawn.transform;
            }
            
            Debug.Log($"[GameStateManager] ✓ Player 1 spawn point: {player1SpawnPoint.position}");
            
            // Player 2 spawn point
            if (player2SpawnPoint == null)
            {
                GameObject player2Spawn = GameObject.Find("Player2SpawnPoint");
                if (player2Spawn == null)
                {
                    player2Spawn = new GameObject("Player2SpawnPoint");
                    player2Spawn.transform.SetParent(spawnPointsParent.transform);
                    player2Spawn.transform.position = new Vector3(10f, 0.5f, 0f);
                    Debug.Log("[GameStateManager] Created Player2SpawnPoint at (10, 0.5, 0)");
                }
                player2SpawnPoint = player2Spawn.transform;
            }
            
            Debug.Log($"[GameStateManager] ✓ Player 2 spawn point: {player2SpawnPoint.position}");
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnRoomJoined -= OnRoomJoined;
                NetworkManager.Instance.OnRoomLeft -= OnRoomLeft;
                NetworkManager.Instance.OnPlayerActionReceived -= OnPlayerActionReceived;
                NetworkManager.Instance.OnGameStateUpdated -= OnGameStateUpdated;
            }
        }
    }
}
