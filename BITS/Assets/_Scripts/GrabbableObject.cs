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

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
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
//             Debug.Log($"Object {gameObject.name} grabbed by player {playerId}");
        }

        public void OnReleased()
        {
            isGrabbed = false;
            currentHolderId = null;
//             Debug.Log($"Object {gameObject.name} released");
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
