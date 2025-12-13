using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Example setup script to quickly create a test scene
    /// Attach this to an empty GameObject in your scene
    /// </summary>
    public class ExampleSetup : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject interactableBoxPrefab;
        
        [Header("Scene Setup")]
        [SerializeField] private bool createFloor = true;
        [SerializeField] private bool createWalls = true;
        [SerializeField] private bool createTestObjects = true;
        [SerializeField] private int numberOfTestObjects = 5;

        private void Start()
        {
            if (createFloor)
                CreateFloor();
            
            if (createWalls)
                CreateWalls();
            
            if (createTestObjects)
                CreateTestObjects();
        }

        private void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(5, 1, 5);
            
            // Add physics material for better interaction
            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.3f, 0.3f, 0.3f);
            }
        }

        private void CreateWalls()
        {
            // Create 4 walls around the play area
            CreateWall("WallNorth", new Vector3(0, 2.5f, 25), new Vector3(50, 5, 1));
            CreateWall("WallSouth", new Vector3(0, 2.5f, -25), new Vector3(50, 5, 1));
            CreateWall("WallEast", new Vector3(25, 2.5f, 0), new Vector3(1, 5, 50));
            CreateWall("WallWest", new Vector3(-25, 2.5f, 0), new Vector3(1, 5, 50));
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = scale;
            
            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.5f, 0.5f, 0.5f);
            }
        }

        private void CreateTestObjects()
        {
            for (int i = 0; i < numberOfTestObjects; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-10f, 10f),
                    1f,
                    Random.Range(-10f, 10f)
                );

                GameObject obj;
                
                if (interactableBoxPrefab != null)
                {
                    obj = Instantiate(interactableBoxPrefab, randomPos, Quaternion.identity);
                }
                else
                {
                    // Create a simple cube if no prefab is assigned
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.transform.position = randomPos;
                    obj.transform.localScale = Vector3.one;
                    
                    // Add Rigidbody
                    var rb = obj.AddComponent<Rigidbody>();
                    rb.mass = 1f;
                    
                    // Add InteractableObject component
                    var interactable = obj.AddComponent<InteractableObject>();
                    
                    // Random color
                    var renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = Random.ColorHSV();
                    }
                }
                
                obj.name = $"InteractableObject_{i}";
                obj.layer = LayerMask.NameToLayer("Default"); // Set to interactable layer
            }
        }

        [ContextMenu("Clear Test Objects")]
        private void ClearTestObjects()
        {
            // Find and destroy all test objects
            var objects = FindObjectsOfType<InteractableObject>();
            foreach (var obj in objects)
            {
                DestroyImmediate(obj.gameObject);
            }
            
            // Destroy floor and walls
            var floor = GameObject.Find("Floor");
            if (floor != null) DestroyImmediate(floor);
            
            var walls = new[] { "WallNorth", "WallSouth", "WallEast", "WallWest" };
            foreach (var wallName in walls)
            {
                var wall = GameObject.Find(wallName);
                if (wall != null) DestroyImmediate(wall);
            }
        }
    }
}
