using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Quick test player setup - creates a simple player you can control immediately
    /// No network, no Input System setup required - just WASD and go!
    /// </summary>
    public class QuickTestPlayer : MonoBehaviour
    {
        [Header("Quick Setup")]
        [SerializeField] private bool createOnStart = true;
        [SerializeField] private Vector3 spawnPosition = new Vector3(0, 1, 0);
        [SerializeField] private Color playerColor = Color.blue;

        private GameObject player;

        private void Start()
        {
            if (createOnStart)
            {
                CreateTestPlayer();
            }
        }

        [ContextMenu("Create Test Player")]
        public void CreateTestPlayer()
        {
            // Create player capsule
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "TestPlayer";
            player.transform.position = spawnPosition;

            // Add Rigidbody
            Rigidbody rb = player.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.freezeRotation = true; // Important!

            // Add SimplePlayerController
            player.AddComponent<SimplePlayerController>();

            // Set color
            Renderer renderer = player.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = playerColor;
                renderer.material = mat;
            }

            Debug.Log("✓ Test player created! Use WASD to move, E to grab, C to cut, B to break");
        }

        [ContextMenu("Delete Test Player")]
        public void DeleteTestPlayer()
        {
            if (player != null)
            {
                DestroyImmediate(player);
                Debug.Log("Test player deleted");
            }
        }
    }
}
