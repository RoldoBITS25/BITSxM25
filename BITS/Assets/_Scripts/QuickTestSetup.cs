using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Quick scene setup for testing the simple player controller
    /// Add this to an empty GameObject in your scene and click Play!
    /// </summary>
    public class QuickTestSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private Color playerColor = Color.blue;

        private void Start()
        {
            if (autoSetupOnStart)
            {
                SetupTestScene();
            }
        }

        [ContextMenu("Setup Test Scene")]
        public void SetupTestScene()
        {
            Debug.Log("Setting up test scene...");

            // Create floor
            CreateFloor();

            // Create player
            CreatePlayer();

            // Create some test objects
            CreateTestObjects();

            // Setup camera
            SetupCamera();

            Debug.Log("Test scene ready! Use WASD to move, E to grab, C to cut, B to break");
        }

        private void CreateFloor()
        {
            var floor = GameObject.Find("Floor");
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Floor";
                floor.transform.position = Vector3.zero;
                floor.transform.localScale = new Vector3(5, 1, 5);
                
                var renderer = floor.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(0.3f, 0.3f, 0.3f);
                    renderer.material = mat;
                }
            }
        }

        private void CreatePlayer()
        {
            var existingPlayer = GameObject.Find("TestPlayer");
            if (existingPlayer != null)
            {
                Debug.Log("Player already exists");
                return;
            }

            // Create player capsule
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "TestPlayer";
            player.transform.position = new Vector3(0, 1, 0);
            
            // Add Rigidbody
            var rb = player.AddComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.mass = 1f;
            
            // Add SimplePlayerController
            player.AddComponent<SimplePlayerController>();
            
            // Set color
            var renderer = player.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = playerColor;
                renderer.material = mat;
            }

            Debug.Log("Player created! Use WASD to move");
        }

        private void CreateTestObjects()
        {
            // Create a few interactable cubes
            for (int i = 0; i < 5; i++)
            {
                Vector3 pos = new Vector3(
                    Random.Range(-8f, 8f),
                    0.5f,
                    Random.Range(-8f, 8f)
                );

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"InteractableBox_{i}";
                cube.transform.position = pos;
                cube.transform.localScale = Vector3.one;

                // Add Rigidbody
                var rb = cube.AddComponent<Rigidbody>();
                rb.mass = 1f;

                // Add SimpleInteractable (works without network!)
                var interactable = cube.AddComponent<SimpleInteractable>();

                // Random color
                var renderer = cube.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var mat = new Material(Shader.Find("Standard"));
                    Color randomColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                    mat.color = randomColor;
                    renderer.material = mat;
                    
                    // Set the interactable's normal color to match
                    interactable.GetType().GetField("normalColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(interactable, randomColor);
                }
            }

            Debug.Log("Created 5 interactable objects");
        }

        private void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 15, -15);
                mainCam.transform.rotation = Quaternion.Euler(45, 0, 0);
            }
        }

        [ContextMenu("Clear Test Scene")]
        public void ClearTestScene()
        {
            // Clear player
            var player = GameObject.Find("TestPlayer");
            if (player != null) DestroyImmediate(player);

            // Clear floor
            var floor = GameObject.Find("Floor");
            if (floor != null) DestroyImmediate(floor);

            // Clear interactable objects
            var simpleObjects = FindObjectsOfType<SimpleInteractable>();
            foreach (var obj in simpleObjects)
            {
                DestroyImmediate(obj.gameObject);
            }
            
            var complexObjects = FindObjectsOfType<InteractableObject>();
            foreach (var obj in complexObjects)
            {
                DestroyImmediate(obj.gameObject);
            }

            Debug.Log("Test scene cleared");
        }
    }
}
