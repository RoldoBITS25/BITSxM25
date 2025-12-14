using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Ultra-simple interactable object for testing
    /// No network dependencies - just works!
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SimpleInteractable : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;
        
        private Renderer objectRenderer;
        private Rigidbody rb;
        private Material material;
        private bool isHighlighted = false;
        private bool isGrabbed = false;

        private void Awake()
        {
            objectRenderer = GetComponent<Renderer>();
            rb = GetComponent<Rigidbody>();
            
            // Create material instance
            if (objectRenderer != null)
            {
                material = objectRenderer.material;
                material.color = normalColor;
            }
        }

        public void SetHighlight(bool highlight)
        {
            isHighlighted = highlight;
            if (material != null)
            {
                material.color = highlight ? highlightColor : normalColor;
            }
        }

        public void OnGrabbed()
        {
            isGrabbed = true;
            if (rb != null)
            {
                rb.isKinematic = true;
            }
//             Debug.Log($"{name} grabbed!");
        }

        public void OnReleased()
        {
            isGrabbed = false;
            if (rb != null)
            {
                rb.isKinematic = false;
            }
//             Debug.Log($"{name} released!");
        }

        public void OnCut()
        {
//             Debug.Log($"{name} cut!");
            
            // Create two smaller pieces
            Vector3 offset = transform.right * 0.5f;
            
            // Piece 1
            GameObject piece1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece1.transform.position = transform.position - offset;
            piece1.transform.localScale = transform.localScale * 0.5f;
            piece1.AddComponent<Rigidbody>();
            var interactable1 = piece1.AddComponent<SimpleInteractable>();
            interactable1.normalColor = normalColor;
            
            // Piece 2
            GameObject piece2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece2.transform.position = transform.position + offset;
            piece2.transform.localScale = transform.localScale * 0.5f;
            piece2.AddComponent<Rigidbody>();
            var interactable2 = piece2.AddComponent<SimpleInteractable>();
            interactable2.normalColor = normalColor;
            
            // Destroy original
            Destroy(gameObject);
        }

        public void OnBreak()
        {
//             Debug.Log($"{name} broken!");
            Destroy(gameObject);
        }

        public bool CanBeGrabbed() => !isGrabbed;
        public bool CanBeCut() => !isGrabbed;
        public bool CanBeBroken() => true;
    }
}
