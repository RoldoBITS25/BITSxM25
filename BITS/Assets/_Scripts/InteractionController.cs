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
            // Get Input System component
            playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                interactAction = playerInput.actions["Interact"];
            }
        }

        private void OnEnable()
        {
            if (interactAction != null)
                interactAction.performed += OnInteractPerformed;
        }

        private void OnDisable()
        {
            if (interactAction != null)
                interactAction.performed -= OnInteractPerformed;
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            // Check for nearby interactable objects
            CheckForInteractables();
        }

        private void CheckForInteractables()
        {
            // Use 2D physics for overlap detection
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, interactableLayer);
            
            IGrabbable closestGrabbable = null;
            ICuttable closestCuttable = null;
            IBreakable closestBreakable = null;
            float closestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                
                // Check for grabbable objects
                var grabbable = collider.GetComponent<IGrabbable>();
                if (grabbable != null && grabbable != heldObject && distance < closestDistance)
                {
                    closestGrabbable = grabbable;
                    closestDistance = distance;
                }
                
                // Check for cuttable objects
                var cuttable = collider.GetComponent<ICuttable>();
                if (cuttable != null && distance < closestDistance)
                {
                    closestCuttable = cuttable;
                }
                
                // Check for breakable objects
                var breakable = collider.GetComponent<IBreakable>();
                if (breakable != null && distance < closestDistance)
                {
                    closestBreakable = breakable;
                }
            }

            // Update grabbable target highlight
            if (targetGrabbable != closestGrabbable)
            {
                if (targetGrabbable is GrabbableObject oldGrab)
                    oldGrab.SetHighlight(false);
                
                targetGrabbable = closestGrabbable;
                
                if (targetGrabbable is GrabbableObject newGrab)
                    newGrab.SetHighlight(true);
            }
            
            // Update cuttable target highlight
            if (targetCuttable != closestCuttable)
            {
                if (targetCuttable is CuttableObject oldCut)
                    oldCut.SetHighlight(false);
                
                targetCuttable = closestCuttable;
                
                if (targetCuttable is CuttableObject newCut)
                    newCut.SetHighlight(true);
            }
            
            // Update breakable target highlight
            if (targetBreakable != closestBreakable)
            {
                if (targetBreakable is BreakableObject oldBreak)
                    oldBreak.SetHighlight(false);
                
                targetBreakable = closestBreakable;
                
                if (targetBreakable is BreakableObject newBreak)
                    newBreak.SetHighlight(true);
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            // Priority 1: Release held object if holding something
            if (heldObject != null)
            {
                ReleaseObject();
                return;
            }

            // Priority 2: Grab object if available
            if (targetGrabbable != null && targetGrabbable.CanBeGrabbed())
            {
                GrabObject(targetGrabbable);
                return;
            }

            // Priority 3: Cut object if available
            if (targetCuttable != null && targetCuttable.CanBeCut())
            {
                Vector2 cutPosition = targetCuttable.GetTransform().position;
                targetCuttable.OnCut(cutPosition);
                
                // Send cut action to network (if needed)
                // NetworkManager.Instance?.SendCutAction(objectId, cutPosition);
                
                targetCuttable = null;
                return;
            }

            // Priority 4: Break object if available
            if (targetBreakable != null && targetBreakable.CanBeBroken())
            {
                string objectId = targetBreakable.GetObjectId();
                targetBreakable.OnBreak();
                
                // Send break action to network (if needed)
                NetworkManager.Instance?.SendBreakAction(objectId);
                
                targetBreakable = null;
                return;
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

        private void OnDrawGizmosSelected()
        {
            // Draw interaction range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}
