using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Component for objects that can be grabbed and moved by the player
    /// </summary>
    public class MovableObject : MonoBehaviour, IInteractable, IGrabbable
    {
        [Header("Movable Settings")]
        [SerializeField] private bool canBeGrabbed = true;
        [SerializeField] private Color highlightColor = Color.yellow;
        
        [Header("Position Settings")]
        [Tooltip("Offset from player position when grabbed (Y is up)")]
        [SerializeField] private Vector3 grabOffset = new Vector3(0, 2f, 0);

        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private bool isGrabbed = false;
        private string currentHolderId;
        private string objectId;
        private Rigidbody rb;
        private bool wasKinematic;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            
            rb = GetComponent<Rigidbody>();

            // Don't auto-generate ID here - wait for explicit initialization
            // This allows for deterministic IDs based on position
        }

        /// <summary>
        /// Initialize object with position-based deterministic ID
        /// Call this after the object is positioned in the scene
        /// </summary>
        public void InitializeWithPosition()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                // Generate deterministic ID based on position
                Vector3 pos = transform.position;
                objectId = $"MovableObject_{pos.x:F2}_{pos.y:F2}_{pos.z:F2}";
                
                // Register with GameStateManager if available
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.RegisterObject(objectId, gameObject);
                }
            }
        }

        /// <summary>
        /// Set a custom object ID (for backend-assigned IDs or other use cases)
        /// </summary>
        public void SetObjectId(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                objectId = id;
                
                // Register with GameStateManager if available
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.RegisterObject(objectId, gameObject);
                }
            }
        }

        public bool CanBeGrabbed()
        {
            return canBeGrabbed && !isGrabbed;
        }

        public void OnGrabbed(string playerId)
        {
            isGrabbed = true;
            currentHolderId = playerId;
            Debug.Log($"Object {gameObject.name} (ID: {objectId}) grabbed by player {playerId}");

            // Disable physics while grabbed
            if (rb != null)
            {
                wasKinematic = rb.isKinematic;
                rb.isKinematic = true;
            }

            // Send grab action to backend
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendGrabAction(objectId);
            }
        }

        public void OnReleased()
        {
            isGrabbed = false;
            string previousHolder = currentHolderId;
            currentHolderId = null;
            Debug.Log($"Object {gameObject.name} (ID: {objectId}) released by player {previousHolder}");

            // Re-enable physics
            if (rb != null)
            {
                rb.isKinematic = wasKinematic;
            }

            // Send release action to backend
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendReleaseAction(objectId);
            }
        }

        public string GetObjectId()
        {
            return objectId;
        }

        public Transform GetTransform()
        {
            return transform;
        }
        
        public Vector3 GetGrabOffset()
        {
            return grabOffset;
        }

        public void SetHighlight(bool highlighted)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = highlighted ? highlightColor : originalColor;
            }
        }
        
        // IInteractable implementation
        public void Interact()
        {
            Debug.Log("Interacting with Movable Object");
            // Interaction is handled by InteractionController
        }
    }
}
