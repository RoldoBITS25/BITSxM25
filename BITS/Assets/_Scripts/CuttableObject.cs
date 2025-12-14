using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Component for objects that can be cut
    /// </summary>
    //[RequireComponent(typeof(Collider2D))]
    public class CuttableObject : MonoBehaviour, ICuttable
    {
        [Header("Cuttable Settings")]
        [SerializeField] private bool canBeCut = true;
        [SerializeField] private Color highlightColor = Color.cyan;
        [SerializeField] private GameObject cutEffectPrefab;

        [Header("Split Settings")]
        [Tooltip("If true, object will split into two halves when cut")]
        [SerializeField] private bool isSplittable = false;
        
        [Tooltip("Force applied to separate the halves")]
        [SerializeField] private float separationForce = 5f;
        
        [Tooltip("Torque applied to make halves rotate")]
        [SerializeField] private float rotationTorque = 3f;

        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private bool hasBeenCut = false;
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
                objectId = $"CuttableObject_{pos.x:F2}_{pos.y:F2}_{pos.z:F2}";
                
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

        public bool CanBeCut()
        {
            return canBeCut && !hasBeenCut;
        }

        public void OnCut(Vector2 cutPosition)
        {
            hasBeenCut = true;
            Debug.Log($"Object {gameObject.name} (ID: {objectId}) cut at position {cutPosition}");

            // Send cut action to backend
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendCutAction(objectId, cutPosition);
            }

            // Spawn cut effect if available
            if (cutEffectPrefab != null)
            {
                Instantiate(cutEffectPrefab, cutPosition, Quaternion.identity);
            }

            // Split into halves if splittable
            if (isSplittable)
            {
                CreateSplitHalves();
            }

            // Destroy the object after cutting
            Destroy(gameObject, 0.1f);
        }

        private void CreateSplitHalves()
        {
            // Create left half
            GameObject leftHalf = CreateHalf("LeftHalf", Vector3.left);
            
            // Create right half
            GameObject rightHalf = CreateHalf("RightHalf", Vector3.right);
            
            // Apply physics to separate the halves
            ApplyPhysicsToHalf(leftHalf, Vector3.left);
            ApplyPhysicsToHalf(rightHalf, Vector3.right);
        }

        private GameObject CreateHalf(string halfName, Vector3 direction)
        {
            GameObject half = new GameObject($"{gameObject.name}_{halfName}");
            half.transform.position = transform.position;
            half.transform.rotation = transform.rotation;
            
            // Scale to half size
            Vector3 scale = transform.localScale;
            scale.x *= 0.5f;
            half.transform.localScale = scale;
            
            // Offset position slightly
            half.transform.position += direction * (scale.x * 0.5f);
            
            // Copy visual components
            CopyVisualComponents(half);
            
            // Add physics components
            Rigidbody rb = half.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.useGravity = false; // Top-down game
            
            BoxCollider collider = half.AddComponent<BoxCollider>();
            
            // Add GrabbableObject component so halves can be grabbed
            var grabbable = half.AddComponent<GrabbableObject>();
            
            // Add CuttableObject component but make it NON-CUTTABLE
            var cuttable = half.AddComponent<CuttableObject>();
            // Use reflection to set canBeCut to false since it's private
            var cuttableType = typeof(CuttableObject);
            var canBeCutField = cuttableType.GetField("canBeCut", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (canBeCutField != null)
            {
                canBeCutField.SetValue(cuttable, false);
            }
            
            // Initialize with deterministic ID based on new position
            // Wait a frame for position to settle
            StartCoroutine(InitializeHalfAfterFrame(grabbable, cuttable));
            
            return half;
        }

        private System.Collections.IEnumerator InitializeHalfAfterFrame(GrabbableObject grabbable, CuttableObject cuttable)
        {
            yield return null; // Wait one frame
            
            if (grabbable != null)
            {
                grabbable.InitializeWithPosition();
            }
            
            if (cuttable != null)
            {
                cuttable.InitializeWithPosition();
            }
        }

        private void CopyVisualComponents(GameObject target)
        {
            // Copy SpriteRenderer if exists
            if (spriteRenderer != null)
            {
                SpriteRenderer newRenderer = target.AddComponent<SpriteRenderer>();
                newRenderer.sprite = spriteRenderer.sprite;
                newRenderer.color = spriteRenderer.color;
                newRenderer.sortingLayerName = spriteRenderer.sortingLayerName;
                newRenderer.sortingOrder = spriteRenderer.sortingOrder;
            }
            
            // Copy MeshRenderer and MeshFilter if exists
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            
            if (meshRenderer != null && meshFilter != null)
            {
                MeshRenderer newMeshRenderer = target.AddComponent<MeshRenderer>();
                newMeshRenderer.material = new Material(meshRenderer.material);
                
                MeshFilter newMeshFilter = target.AddComponent<MeshFilter>();
                newMeshFilter.mesh = meshFilter.mesh;
            }
        }

        private void ApplyPhysicsToHalf(GameObject half, Vector3 direction)
        {
            Rigidbody rb = half.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply separation force
                rb.AddForce(direction * separationForce, ForceMode.Impulse);
                
                // Apply random torque for realistic tumbling
                Vector3 randomTorque = new Vector3(
                    Random.Range(-rotationTorque, rotationTorque),
                    Random.Range(-rotationTorque, rotationTorque),
                    Random.Range(-rotationTorque, rotationTorque)
                );
                rb.AddTorque(randomTorque, ForceMode.Impulse);
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
