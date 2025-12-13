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
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
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
            foreach (string playerId in room.current_players)
            {
                if (playerId != NetworkManager.Instance.PlayerId && !spawnedPlayers.ContainsKey(playerId))
                {
                    SpawnRemotePlayer(playerId);
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
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab not assigned!");
                return;
            }

            Transform spawnPoint = NetworkManager.Instance.PlayerNumber == 1 ? player1SpawnPoint : player2SpawnPoint;
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;

            localPlayerObject = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            localPlayerObject.name = $"LocalPlayer_{NetworkManager.Instance.PlayerId}";

            // Enable player controller
            var controller = localPlayerObject.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.Initialize(NetworkManager.Instance.PlayerId, true);
            }

            // Set player color based on player number
            SetPlayerColor(localPlayerObject, NetworkManager.Instance.PlayerNumber);
        }

        private void SpawnRemotePlayer(string playerId)
        {
            if (playerPrefab == null || spawnedPlayers.ContainsKey(playerId))
                return;

            // Determine spawn position based on player index
            int playerIndex = 0;
            // This would need to be determined from room data
            Transform spawnPoint = playerIndex == 0 ? player1SpawnPoint : player2SpawnPoint;
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;

            GameObject remotePlayer = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            remotePlayer.name = $"RemotePlayer_{playerId}";

            // Disable local control for remote players
            var controller = remotePlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.Initialize(playerId, false);
            }

            spawnedPlayers[playerId] = remotePlayer;
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
                Color color = playerNumber == 1 ? Color.blue : Color.red;
                renderer.material.color = color;
            }
        }

        private void OnPlayerActionReceived(PlayerAction action)
        {
            // Handle actions from other players
            if (action.player_id == NetworkManager.Instance.PlayerId)
                return; // Ignore our own actions

            switch (action.action_type)
            {
                case "move":
                    UpdatePlayerPosition(action.player_id, action.position);
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
            }
        }

        private void UpdatePlayerPosition(string playerId, Vector3 position)
        {
            if (spawnedPlayers.TryGetValue(playerId, out GameObject player))
            {
                player.transform.position = position;
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
