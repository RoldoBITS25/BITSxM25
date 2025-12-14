using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Component for objects that can be grabbed and held by the player
    /// </summary>
    //[RequireComponent(typeof(Collider2D))]
    public class GrabbableObject : MonoBehaviour, IGrabbable
    {
        [Header("Grabbable Settings")]
        [SerializeField] private bool canBeGrabbed = true;
        [SerializeField] private Color highlightColor = Color.yellow;

        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private bool isGrabbed = false;
        private string currentHolderId;
        private string objectId;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

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
                objectId = $"GrabbableObject_{pos.x:F2}_{pos.y:F2}_{pos.z:F2}";
                
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

        public void SetHighlight(bool highlighted)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = highlighted ? highlightColor : originalColor;
            }
        }
    }
}
