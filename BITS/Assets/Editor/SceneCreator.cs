using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace MultiplayerGame.Editor
{
    /// <summary>
    /// Creates a complete test scene with one menu click
    /// Menu: Tools > Create Test Scene
    /// </summary>
    public class SceneCreator
    {
        [MenuItem("Tools/Create Test Scene")]
        public static void CreateTestScene()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            Debug.Log("Creating test scene...");
            
            // Create floor
            CreateFloor();
            
            // Create player
            CreatePlayer();
            
            // Create test objects
            CreateTestObjects();
            
            // Setup camera
            SetupCamera();
            
            // Save scene
            string scenePath = "Assets/Scenes/TestScene.unity";
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"Test scene created at {scenePath}");
            Debug.Log("Press PLAY to test! Use WASD to move, E to grab, C to cut, B to break");
            
            // Auto-play
            EditorApplication.isPlaying = true;
        }
        
        private static void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(5, 1, 5);
            
            var renderer = floor.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.3f, 0.3f, 0.3f);
            renderer.material = mat;
            
            Debug.Log("✓ Floor created");
        }
        
        private static void CreatePlayer()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = new Vector3(0, 1, 0);
            
            // Add Rigidbody
            var rb = player.AddComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.mass = 1f;
            
            // Add SimplePlayerController
            player.AddComponent<SimplePlayerController>();
            
            // Set color
            var renderer = player.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.blue;
            renderer.material = mat;
            
            Debug.Log("✓ Player created (blue capsule)");
        }
        
        private static void CreateTestObjects()
        {
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

                // Add SimpleInteractable
                cube.AddComponent<SimpleInteractable>();

                // Random color
                var renderer = cube.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                Color randomColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
                mat.color = randomColor;
                renderer.material = mat;
            }
            
            Debug.Log("✓ 5 interactable objects created");
        }
        
        private static void SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 15, -15);
                mainCam.transform.rotation = Quaternion.Euler(45, 0, 0);
                Debug.Log("✓ Camera positioned");
            }
        }
    }
}
