using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame
{
    /// <summary>
    /// Handles all object interaction logic (grab, cut, break)
    /// Detects nearby interactable objects and manages interaction state
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private LayerMask interactableLayer;

        private string playerId;
        private bool isLocalPlayer = true;
        
        // Interface-based interaction targets
        private IGrabbable heldObject;
        private IGrabbable targetGrabbable;
        private ICuttable targetCuttable;
        private IBreakable targetBreakable;

        // Input
        private PlayerInput playerInput;
        private InputAction interactAction;

        public void Initialize(string playerId, bool isLocal)
        {
            this.playerId = playerId;
            this.isLocalPlayer = isLocal;

            if (!isLocal)
            {
                // Disable input for remote players
                if (playerInput != null)
                    playerInput.enabled = false;
            }
        }

        private void Awake()
        {
      
        }

        private void OnEnable()
        {
           
        }

        private void OnDisable()
        {
           
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                Debug.Log("Interacting");
                CheckForInteractables();
            }

        }


        private void CheckForInteractables()
        {
            // Get the nearest game object within interaction range
            GameObject nearestObject = GetNearestGameObject(interactionRange);
            
            if (nearestObject == null)
                return;
            
            // Check if the nearest object is interactable
            var interactable = nearestObject.GetComponent<IInteractable>();
            if (interactable != null)
            {
                Debug.Log($"Interacting with {nearestObject.name}");
                interactable.Interact();
            }
        }


        private void GrabObject(IGrabbable obj)
        {
            if (obj.CanBeGrabbed())
            {
                heldObject = obj;
                obj.OnGrabbed(playerId);
                
                // Send grab action to network (if needed)
                // NetworkManager.Instance?.SendGrabAction(objectId);
                
                // Attach object to player
                Transform objTransform = obj.GetTransform();
                objTransform.SetParent(transform);
                objTransform.localPosition = new Vector2(0, 1);
            }
        }

        private void ReleaseObject()
        {
            if (heldObject != null)
            {
                Transform objTransform = heldObject.GetTransform();
                heldObject.OnReleased();
                objTransform.SetParent(null);
                
                // Send release action (could be a "move" action with the drop position)
                NetworkManager.Instance?.SendMoveAction(objTransform.position);
                
                heldObject = null;
            }
        }
        
        public GameObject GetNearestGameObject(float range)
        {
            // Get all colliders within range (no layer filtering)
            Collider[] colliders = Physics.OverlapSphere(transform.position, range);
            
            GameObject nearestObject = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var collider in colliders)
            {
                // Skip self
                if (collider.gameObject == gameObject)
                    continue;
                
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestObject = collider.gameObject;
                }
            }
            
            return nearestObject;
        }
    }
}
