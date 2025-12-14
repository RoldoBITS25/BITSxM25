using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Component for objects that can be broken and drop items
    /// </summary>
    //[RequireComponent(typeof(Collider2D))]
    public class BreakableObject : MonoBehaviour, IBreakable
    {
        [Header("Breakable Settings")]
        [SerializeField] private bool canBeBroken = true;
        [SerializeField] private Color highlightColor = Color.red;
        
        [Header("Drop Settings")]
        [Tooltip("GameObject to spawn when this object is broken")]
        [SerializeField] private GameObject dropPrefab;
        [SerializeField] private int dropCount = 1;
        [SerializeField] private float dropForce = 2f;
        [SerializeField] private float dropSpread = 0.5f;

        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private bool hasBeenBroken = false;
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
                objectId = $"BreakableObject_{pos.x:F2}_{pos.y:F2}_{pos.z:F2}";
                
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

        public bool CanBeBroken()
        {
            return canBeBroken && !hasBeenBroken;
        }

        public void OnBreak()
        {
            hasBeenBroken = true;
            Debug.Log($"Object {gameObject.name} (ID: {objectId}) broken");

            // Send break action to backend
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendBreakAction(objectId);
            }

            // Spawn drop items if prefab is assigned
            if (dropPrefab != null)
            {
                for (int i = 0; i < dropCount; i++)
                {
                    SpawnDropItem();
                }
            }

            // Destroy the breakable object
            Destroy(gameObject, 0.1f);
        }

        private void SpawnDropItem()
        {
            // Calculate random offset for spread
            Vector2 randomOffset = Random.insideUnitCircle * dropSpread;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);

            // Instantiate the drop
            GameObject drop = Instantiate(dropPrefab, spawnPosition, Quaternion.identity);

            // Apply force if the drop has a Rigidbody2D
            Rigidbody2D rb = drop.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                rb.AddForce(randomDirection * dropForce, ForceMode2D.Impulse);
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
