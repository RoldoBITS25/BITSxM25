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

        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private bool hasBeenCut = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        public bool CanBeCut()
        {
            return canBeCut && !hasBeenCut;
        }

        public void OnCut(Vector2 cutPosition)
        {
            hasBeenCut = true;
            Debug.Log($"Object {gameObject.name} cut at position {cutPosition}");

            // Spawn cut effect if available
            if (cutEffectPrefab != null)
            {
                Instantiate(cutEffectPrefab, cutPosition, Quaternion.identity);
            }

            // Destroy the object after cutting
            Destroy(gameObject, 0.1f);
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
