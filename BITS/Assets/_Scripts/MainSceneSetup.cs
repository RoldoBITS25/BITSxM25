using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Main scene setup for top-down multiplayer game
    /// Attach this to an empty GameObject in your main scene
    /// This will automatically configure the scene for multiplayer gameplay
    /// </summary>
    public class MainSceneSetup : MonoBehaviour
    {
        [Header("Scene Configuration")]
        [SerializeField] public bool autoSetupOnStart = true;
        [SerializeField] private bool waitForRoomJoin = false; // If true, wait for MainMenuManager
        [SerializeField] private bool setupCamera = true;
        [SerializeField] private bool setupLighting = true;
        [SerializeField] private bool setupEnvironment = true;
        [SerializeField] private bool setupNetworking = true;
        
        [Header("Camera Settings")]
        [SerializeField] private float cameraHeight = 20f;
        [SerializeField] private float cameraAngle = 60f; // Top-down angle
        [SerializeField] private float cameraDistance = 15f;
        [SerializeField] private Vector3 cameraFocusPoint = Vector3.zero;
        
        [Header("Environment Settings")]
        [SerializeField] private Vector2 playAreaSize = new Vector2(40f, 40f);
        [SerializeField] private Color floorColor = new Color(0.2f, 0.3f, 0.2f);
        [SerializeField] private Color wallColor = new Color(0.4f, 0.4f, 0.4f);
        [SerializeField] private float wallHeight = 5f;
        
        [Header("Networking Settings")]
        [SerializeField] private bool useLocalhostInBuild = false;
        [SerializeField] private string serverUrl = "http://127.0.0.1:8000";
        [SerializeField] private string wsUrl = "ws://127.0.0.1:8000";
        
        [Header("Prefab References")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject spectatorCameraPrefab;
        [SerializeField] private GameObject interactableObjectPrefab;
        [SerializeField] private GameObject multiplayerDebugUIPrefab;
        
        [Header("Test Objects")]
        [SerializeField] private bool spawnTestObjects = true;
        [SerializeField] private int numberOfTestObjects = 10;
        
        [Header("Test Player")]
        [SerializeField] private bool spawnTestPlayer = true;
        [SerializeField] private Vector3 testPlayerSpawnPosition = new Vector3(0, 1, 0);
        [SerializeField] private Color testPlayerColor = Color.cyan;

        [Header("Debug")]
        [SerializeField] private bool spawnDebugUI = true;

        private GameObject networkManagerObj;
        private GameObject gameStateManagerObj;
        private GameObject mainCameraObj;
        private GameObject environmentObj;
        private GameObject testPlayerObj;

        private void Start()
        {
            if (autoSetupOnStart && !waitForRoomJoin)
            {
                SetupScene();
            }
            else if (waitForRoomJoin)
            {
                Debug.Log("[MainSceneSetup] Waiting for room join before setup...");
            }
        }

        [ContextMenu("Setup Scene")]
        public void SetupScene()
        {
            Debug.Log("Setting up main multiplayer scene...");
            
            if (setupCamera)
                SetupTopDownCamera();
            
            if (setupLighting)
                SetupLighting();
            
            if (setupEnvironment)
                SetupEnvironment();
            
            if (setupNetworking)
                SetupNetworkingComponents();

            if (spawnDebugUI)
                SetupDebugUI();
            
            if (spawnTestObjects)
                SpawnTestObjects();
            
            if (spawnTestPlayer)
                SpawnTestPlayer();
            
            Debug.Log("Main scene setup complete!");
        }

        private void SetupDebugUI()
        {
            if (GameObject.FindObjectOfType<MultiplayerDebugUI>() == null)
            {
                if (multiplayerDebugUIPrefab != null)
                {
                    Instantiate(multiplayerDebugUIPrefab);
                    Debug.Log("[MainSceneSetup] Spawned Debug UI from prefab");
                }
                else
                {
                    // Create programmatically if no prefab
                    GameObject debugObj = new GameObject("MultiplayerDebugUI");
                    var debugUI = debugObj.AddComponent<MultiplayerDebugUI>();
                    debugUI.CreateDebugUI();
                    Debug.Log("[MainSceneSetup] Created Debug UI programmatically");
                }
            }
        }

        private void SetupTopDownCamera()
        {
            // Find or create main camera
            mainCameraObj = GameObject.FindGameObjectWithTag("MainCamera");
            
            if (mainCameraObj == null)
            {
                mainCameraObj = new GameObject("Main Camera");
                mainCameraObj.tag = "MainCamera";
                mainCameraObj.AddComponent<Camera>();
                mainCameraObj.AddComponent<AudioListener>();
            }

            // Position camera for top-down view
            float radians = cameraAngle * Mathf.Deg2Rad;
            float x = cameraFocusPoint.x;
            float y = cameraHeight;
            float z = cameraFocusPoint.z - cameraDistance * Mathf.Cos(radians);
            
            mainCameraObj.transform.position = new Vector3(x, y, z);
            mainCameraObj.transform.LookAt(cameraFocusPoint);
            
            // Configure camera settings for better top-down view
            Camera cam = mainCameraObj.GetComponent<Camera>();
            if (cam != null)
            {
                cam.fieldOfView = 60f;
                cam.farClipPlane = 100f;
                cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            }
            
            Debug.Log($"Camera positioned at {mainCameraObj.transform.position} looking at {cameraFocusPoint}");
        }

        private void SetupLighting()
        {
            // Create directional light for top-down illumination
            GameObject lightObj = GameObject.Find("Directional Light");
            
            if (lightObj == null)
            {
                lightObj = new GameObject("Directional Light");
                lightObj.AddComponent<Light>();
            }
            
            Light light = lightObj.GetComponent<Light>();
            if (light != null)
            {
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1.2f;
                light.shadows = LightShadows.Soft;
                
                // Position light for top-down view (slightly angled)
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
            
            // Add ambient lighting
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.45f);
            
            Debug.Log("Lighting configured for top-down view");
        }

        private void SetupEnvironment()
        {
            // Create environment parent
            environmentObj = GameObject.Find("Environment");
            if (environmentObj == null)
            {
                environmentObj = new GameObject("Environment");
            }
            
            // Create floor
            CreateFloor();
            
            // Create boundary walls
            CreateBoundaryWalls();
            
            Debug.Log("Environment created");
        }

        private void CreateFloor()
        {
            GameObject floor = GameObject.Find("Floor");
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Floor";
                floor.transform.SetParent(environmentObj.transform);
            }
            
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(playAreaSize.x / 10f, 1f, playAreaSize.y / 10f);
            
            // Configure floor material
            Renderer renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material floorMat = new Material(Shader.Find("Standard"));
                floorMat.color = floorColor;
                floorMat.SetFloat("_Metallic", 0.2f);
                floorMat.SetFloat("_Glossiness", 0.3f);
                renderer.material = floorMat;
            }
            
            // Add collider if not present
            if (floor.GetComponent<MeshCollider>() == null)
            {
                floor.AddComponent<MeshCollider>();
            }
        }

        private void CreateBoundaryWalls()
        {
            float halfWidth = playAreaSize.x / 2f;
            float halfDepth = playAreaSize.y / 2f;
            
            // North wall
            CreateWall("Wall_North", 
                new Vector3(0, wallHeight / 2f, halfDepth), 
                new Vector3(playAreaSize.x, wallHeight, 0.5f));
            
            // South wall
            CreateWall("Wall_South", 
                new Vector3(0, wallHeight / 2f, -halfDepth), 
                new Vector3(playAreaSize.x, wallHeight, 0.5f));
            
            // East wall
            CreateWall("Wall_East", 
                new Vector3(halfWidth, wallHeight / 2f, 0), 
                new Vector3(0.5f, wallHeight, playAreaSize.y));
            
            // West wall
            CreateWall("Wall_West", 
                new Vector3(-halfWidth, wallHeight / 2f, 0), 
                new Vector3(0.5f, wallHeight, playAreaSize.y));
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.Find(name);
            if (wall == null)
            {
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = name;
                wall.transform.SetParent(environmentObj.transform);
            }
            
            wall.transform.position = position;
            wall.transform.localScale = scale;
            
            // Configure wall material
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material wallMat = new Material(Shader.Find("Standard"));
                wallMat.color = wallColor;
                wallMat.SetFloat("_Metallic", 0.1f);
                wallMat.SetFloat("_Glossiness", 0.2f);
                renderer.material = wallMat;
            }
        }

        private void SetupNetworkingComponents()
        {
            // Check if NetworkManager already exists (it should with DontDestroyOnLoad)
            NetworkManager netManager = NetworkManager.Instance;
            
            if (netManager == null)
            {
                Debug.LogWarning("[MainSceneSetup] NetworkManager.Instance is null, creating new one");
                // Create or find NetworkManager
                networkManagerObj = GameObject.Find("NetworkManager");
                if (networkManagerObj == null)
                {
                    networkManagerObj = new GameObject("NetworkManager");
                }
                
                netManager = networkManagerObj.GetComponent<NetworkManager>();
                if (netManager == null)
                {
                    netManager = networkManagerObj.AddComponent<NetworkManager>();
                }
                
                // Configure server URLs using reflection since they're private serialized fields
                // Only override if in Editor or if explicitly requested for build (e.g. testing local build)
                if (Application.isEditor || useLocalhostInBuild)
                {
                    Debug.Log($"[MainSceneSetup] Overriding NetworkManager URLs with: {serverUrl} (Localhost/Override)");
                    
                    var netManagerType = typeof(NetworkManager);
                    var serverUrlField = netManagerType.GetField("serverUrl", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var wsUrlField = netManagerType.GetField("wsUrl", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                    if (serverUrlField != null)
                        serverUrlField.SetValue(netManager, serverUrl);
                    if (wsUrlField != null)
                        wsUrlField.SetValue(netManager, wsUrl);
                }
                else
                {
                     Debug.Log("[MainSceneSetup] Using default NetworkManager URLs (Production/Build)");
                }
            }
            else
            {
                Debug.Log("[MainSceneSetup] Using existing NetworkManager instance (DontDestroyOnLoad)");
                networkManagerObj = netManager.gameObject;
            }            
            // Create or find GameStateManager
            gameStateManagerObj = GameObject.Find("GameStateManager");
            if (gameStateManagerObj == null)
            {
                gameStateManagerObj = new GameObject("GameStateManager");
            }
            
            GameStateManager stateManager = gameStateManagerObj.GetComponent<GameStateManager>();
            if (stateManager == null)
            {
                stateManager = gameStateManagerObj.AddComponent<GameStateManager>();
            }
            
            // Set up spawn points
            SetupSpawnPoints();
            
            // Assign prefabs to GameStateManager using reflection
            var stateManagerType = typeof(GameStateManager);
            var playerPrefabField = stateManagerType.GetField("playerPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var spectatorPrefabField = stateManagerType.GetField("spectatorCameraPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (playerPrefabField != null && playerPrefab != null)
            {
                playerPrefabField.SetValue(stateManager, playerPrefab);
                Debug.Log($"[MainSceneSetup] ✓ Assigned player prefab to GameStateManager: {playerPrefab.name}");
            }
            else if (playerPrefab == null)
            {
                Debug.LogWarning("[MainSceneSetup] No player prefab assigned in MainSceneSetup. GameStateManager will create a default one.");
            }
            
            if (spectatorPrefabField != null && spectatorCameraPrefab != null)
            {
                spectatorPrefabField.SetValue(stateManager, spectatorCameraPrefab);
                Debug.Log($"[MainSceneSetup] ✓ Assigned spectator prefab to GameStateManager: {spectatorCameraPrefab.name}");
            }
            else if (spectatorCameraPrefab == null)
            {
                Debug.LogWarning("[MainSceneSetup] No spectator camera prefab assigned in MainSceneSetup.");
            }
            
            Debug.Log("Networking components configured");
        }

        private void SetupSpawnPoints()
        {
            // Create spawn points for players
            GameObject spawnPointsParent = GameObject.Find("SpawnPoints");
            if (spawnPointsParent == null)
            {
                spawnPointsParent = new GameObject("SpawnPoints");
            }
            
            // Player 1 spawn point (left side)
            GameObject player1Spawn = GameObject.Find("Player1SpawnPoint");
            if (player1Spawn == null)
            {
                player1Spawn = new GameObject("Player1SpawnPoint");
                player1Spawn.transform.SetParent(spawnPointsParent.transform);
            }
            player1Spawn.transform.position = new Vector3(-10f, 0.5f, 0f);
            
            // Player 2 spawn point (right side)
            GameObject player2Spawn = GameObject.Find("Player2SpawnPoint");
            if (player2Spawn == null)
            {
                player2Spawn = new GameObject("Player2SpawnPoint");
                player2Spawn.transform.SetParent(spawnPointsParent.transform);
            }
            player2Spawn.transform.position = new Vector3(10f, 0.5f, 0f);
            
            // Assign spawn points to GameStateManager
            GameStateManager stateManager = gameStateManagerObj?.GetComponent<GameStateManager>();
            if (stateManager != null)
            {
                var stateManagerType = typeof(GameStateManager);
                var player1SpawnField = stateManagerType.GetField("player1SpawnPoint", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var player2SpawnField = stateManagerType.GetField("player2SpawnPoint", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (player1SpawnField != null)
                {
                    player1SpawnField.SetValue(stateManager, player1Spawn.transform);
                    Debug.Log($"[MainSceneSetup] ✓ Assigned Player 1 spawn point: {player1Spawn.transform.position}");
                }
                if (player2SpawnField != null)
                {
                    player2SpawnField.SetValue(stateManager, player2Spawn.transform);
                    Debug.Log($"[MainSceneSetup] ✓ Assigned Player 2 spawn point: {player2Spawn.transform.position}");
                }
            }
            else
            {
                Debug.LogWarning("[MainSceneSetup] GameStateManager not found, cannot assign spawn points");
            }
            
            Debug.Log("Spawn points created and assigned");
        }

        private void SpawnTestObjects()
        {
            GameObject testObjectsParent = GameObject.Find("TestObjects");
            if (testObjectsParent == null)
            {
                testObjectsParent = new GameObject("TestObjects");
            }
            
            for (int i = 0; i < numberOfTestObjects; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-playAreaSize.x / 3f, playAreaSize.x / 3f),
                    1f,
                    Random.Range(-playAreaSize.y / 3f, playAreaSize.y / 3f)
                );
                
                GameObject obj;
                
                if (interactableObjectPrefab != null)
                {
                    obj = Instantiate(interactableObjectPrefab, randomPos, Quaternion.identity);
                }
                else
                {
                    // Create a simple interactable cube
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.position = randomPos;
                    obj.transform.localScale = Vector3.one;
                    
                    // Add Rigidbody
                    Rigidbody rb = obj.AddComponent<Rigidbody>();
                    rb.mass = 1f;
                    rb.linearDamping = 0.5f;
                    rb.angularDamping = 0.5f;
                    
                    // Add InteractableObject component
                    obj.AddComponent<InteractableObject>();
                    
                    // Random color
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Material mat = new Material(Shader.Find("Standard"));
                        mat.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                        mat.SetFloat("_Metallic", 0.3f);
                        mat.SetFloat("_Glossiness", 0.5f);
                        renderer.material = mat;
                    }
                }
                
                obj.name = $"InteractableObject_{i:00}";
                obj.transform.SetParent(testObjectsParent.transform);
            }
            
            Debug.Log($"Spawned {numberOfTestObjects} test objects");
        }

        private void SpawnTestPlayer()
        {
            // Create player capsule
            testPlayerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            testPlayerObj.name = "TestPlayer";
            testPlayerObj.transform.position = testPlayerSpawnPosition;

            // Add Rigidbody
            Rigidbody rb = testPlayerObj.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.freezeRotation = true;

            // Add SimplePlayerController
            testPlayerObj.AddComponent<SimplePlayerController>();

            // Set color
            Renderer renderer = testPlayerObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = testPlayerColor;
                renderer.material = mat;
            }

            Debug.Log("✓ Test player spawned! Use WASD to move, E to grab, C to cut, B to break");
        }

        [ContextMenu("Clear Scene")]
        public void ClearScene()
        {
            // Clear test player
            if (testPlayerObj != null)
            {
                DestroyImmediate(testPlayerObj);
            }
            
            // Clear test objects
            GameObject testObjects = GameObject.Find("TestObjects");
            if (testObjects != null)
            {
                DestroyImmediate(testObjects);
            }
            
            // Clear environment
            if (environmentObj != null)
            {
                DestroyImmediate(environmentObj);
            }
            
            Debug.Log("Scene cleared");
        }

        private void OnDrawGizmos()
        {
            // Draw play area bounds
            Gizmos.color = Color.yellow;
            Vector3 center = Vector3.zero;
            Vector3 size = new Vector3(playAreaSize.x, 0.1f, playAreaSize.y);
            Gizmos.DrawWireCube(center, size);
            
            // Draw spawn points
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(new Vector3(-10f, 0.5f, 0f), 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(10f, 0.5f, 0f), 0.5f);
            
            // Draw camera focus point
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(cameraFocusPoint, 0.5f);
        }
    }
}
