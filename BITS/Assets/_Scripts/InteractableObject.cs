using System;
using UnityEngine;

namespace MultiplayerGame
{
    /// <summary>
    /// Base class for objects that can be interacted with
    /// Handles grab, cut, break, and move actions
    /// </summary>
    public class InteractableObject : MonoBehaviour
    {
        [Header("Object Info")]
        [SerializeField] private string objectId;
        [SerializeField] private string objectType = "box";

        [Header("Interaction Settings")]
        [SerializeField] private bool canBeGrabbed = true;
        [SerializeField] private bool canBeCut = true;
        [SerializeField] private bool canBeBroken = true;
        [SerializeField] private bool canBeMoved = true;

        [Header("Visual Feedback")]
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private GameObject breakEffectPrefab;

        private Renderer objectRenderer;
        private Rigidbody rb;
        private bool isGrabbed = false;
        private string grabbedByPlayerId;

        public string ObjectId => objectId;
        public string ObjectType => objectType;
        public bool IsGrabbed => isGrabbed;

        private void Awake()
        {
            // Generate unique ID if not set
            if (string.IsNullOrEmpty(objectId))
            {
                objectId = $"{objectType}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            objectRenderer = GetComponent<Renderer>();
            rb = GetComponent<Rigidbody>();

            // Register with GameStateManager (only if it exists - optional for standalone testing)
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.RegisterObject(objectId, gameObject);
            }
            
            // Ensure Rigidbody exists for physics
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
        }

        private void Start()
        {
            if (normalMaterial == null && objectRenderer != null)
            {
                normalMaterial = objectRenderer.material;
            }
        }

        #region Interaction Checks

        public bool CanBeGrabbed()
        {
            return canBeGrabbed && !isGrabbed;
        }

        public bool CanBeCut()
        {
            return canBeCut && !isGrabbed;
        }

        public bool CanBeBroken()
        {
            return canBeBroken;
        }

        public bool CanBeMoved()
        {
            return canBeMoved;
        }

        #endregion

        #region Action Handlers

        public void OnGrabbed(string playerId)
        {
            if (!canBeGrabbed)
                return;

            isGrabbed = true;
            grabbedByPlayerId = playerId;

            // Disable physics while grabbed
            if (rb != null)
            {
                rb.isKinematic = true;
            }

//             Debug.Log($"{objectId} grabbed by {playerId}");
        }

        public void OnReleased()
        {
            isGrabbed = false;
            grabbedByPlayerId = null;

            // Re-enable physics
            if (rb != null)
            {
                rb.isKinematic = false;
            }

//             Debug.Log($"{objectId} released");
        }

        public void OnCut(Vector3 cutPosition)
        {
            if (!canBeCut)
                return;

//             Debug.Log($"{objectId} cut at {cutPosition}");

            // Create two halves
            CreateCutPieces(cutPosition);

            // Destroy original object
            DestroyObject();
        }

        public void OnBreak()
        {
            if (!canBeBroken)
                return;

//             Debug.Log($"{objectId} broken");

            // Spawn break effect
            if (breakEffectPrefab != null)
            {
                Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
            }

            // Destroy object
            DestroyObject();
        }

        public void OnMoved(Vector3 newPosition)
        {
            if (!canBeMoved)
                return;

            transform.position = newPosition;
        }

        #endregion

        #region Visual Feedback

        public void SetHighlight(bool highlighted)
        {
            if (objectRenderer != null && highlightMaterial != null)
            {
                objectRenderer.material = highlighted ? highlightMaterial : normalMaterial;
            }
        }

        #endregion

        #region Helper Methods

        private void CreateCutPieces(Vector3 cutPosition)
        {
            // Simple implementation: create two smaller versions
            Vector3 halfScale = transform.localScale * 0.5f;
            Vector3 offset = (cutPosition - transform.position).normalized * 0.5f;

            // Create first piece
            GameObject piece1 = Instantiate(gameObject, transform.position - offset, transform.rotation);
            piece1.transform.localScale = halfScale;
            var interactable1 = piece1.GetComponent<InteractableObject>();
            if (interactable1 != null)
            {
                interactable1.objectId = $"{objectId}_piece1";
                interactable1.canBeCut = false; // Pieces can't be cut again
            }

            // Create second piece
            GameObject piece2 = Instantiate(gameObject, transform.position + offset, transform.rotation);
            piece2.transform.localScale = halfScale;
            var interactable2 = piece2.GetComponent<InteractableObject>();
            if (interactable2 != null)
            {
                interactable2.objectId = $"{objectId}_piece2";
                interactable2.canBeCut = false;
            }
        }

        private void DestroyObject()
        {
            // Unregister from GameStateManager
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.UnregisterObject(objectId);
            }

            Destroy(gameObject);
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up (only if GameStateManager exists)
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.UnregisterObject(objectId);
            }
        }
    }
}
