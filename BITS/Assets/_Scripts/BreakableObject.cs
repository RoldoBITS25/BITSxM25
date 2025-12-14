using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Component for objects that can be broken and drop items
    /// </summary>
    //[RequireComponent(typeof(Collider2D))]
    public class BreakableObject : MonoBehaviour
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

            // Generate unique ID for this object
            objectId = System.Guid.NewGuid().ToString();
        }

        public bool CanBeBroken()
        {
            return canBeBroken && !hasBeenBroken;
        }

        public void OnBreak()
        {
            hasBeenBroken = true;
            Debug.Log($"Object {gameObject.name} broken");

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
