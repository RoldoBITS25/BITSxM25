using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame
{
    /// <summary>
    /// Controls player movement and actions
    /// Handles input and sends actions to the network
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private LayerMask interactableLayer;

        private string playerId;
        private bool isLocalPlayer;
        private Rigidbody rb;
        private Vector2 moveInput;
        private InteractableObject heldObject;
        private InteractableObject targetObject;

        // Input Actions (assuming you're using Unity's Input System)
        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction grabAction;
        private InputAction cutAction;
        private InputAction breakAction;

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
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;

            // Get Input System component
            playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                moveAction = playerInput.actions["Move"];
                grabAction = playerInput.actions["Grab"];
                cutAction = playerInput.actions["Cut"];
                breakAction = playerInput.actions["Break"];
            }
        }

        private void OnEnable()
        {
            if (grabAction != null) grabAction.performed += OnGrabPerformed;
            if (cutAction != null) cutAction.performed += OnCutPerformed;
            if (breakAction != null) breakAction.performed += OnBreakPerformed;
        }

        private void OnDisable()
        {
            if (grabAction != null) grabAction.performed -= OnGrabPerformed;
            if (cutAction != null) cutAction.performed -= OnCutPerformed;
            if (breakAction != null) breakAction.performed -= OnBreakPerformed;
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            // Read movement input
            if (moveAction != null)
            {
                moveInput = moveAction.ReadValue<Vector2>();
            }

            // Check for nearby interactable objects
            CheckForInteractables();

            // Send position updates periodically
            if (Time.frameCount % 10 == 0) // Every 10 frames
            {
                NetworkManager.Instance?.SendMoveAction(transform.position);
            }
        }

        private void FixedUpdate()
        {
            if (!isLocalPlayer)
                return;

            // Handle movement
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 movement = new Vector3(moveInput.x, 0, moveInput.y);
                rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

                // Rotate towards movement direction
                if (movement != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(movement);
                    rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                }
            }
        }

        private void CheckForInteractables()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange, interactableLayer);
            
            InteractableObject closest = null;
            float closestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                var interactable = collider.GetComponent<InteractableObject>();
                if (interactable != null && interactable != heldObject)
                {
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closest = interactable;
                        closestDistance = distance;
                    }
                }
            }

            // Update target highlight
            if (targetObject != closest)
            {
                if (targetObject != null)
                    targetObject.SetHighlight(false);
                
                targetObject = closest;
                
                if (targetObject != null)
                    targetObject.SetHighlight(true);
            }
        }

        private void OnGrabPerformed(InputAction.CallbackContext context)
        {
            if (heldObject != null)
            {
                // Release held object
                ReleaseObject();
            }
            else if (targetObject != null)
            {
                // Grab target object
                GrabObject(targetObject);
            }
        }

        private void GrabObject(InteractableObject obj)
        {
            if (obj.CanBeGrabbed())
            {
                heldObject = obj;
                obj.OnGrabbed(playerId);
                
                // Send grab action to network
                NetworkManager.Instance?.SendGrabAction(obj.ObjectId);
                
                // Attach object to player
                obj.transform.SetParent(transform);
                obj.transform.localPosition = new Vector3(0, 1, 1);
            }
        }

        private void ReleaseObject()
        {
            if (heldObject != null)
            {
                heldObject.OnReleased();
                heldObject.transform.SetParent(null);
                
                // Send release action (could be a "move" action with the drop position)
                NetworkManager.Instance?.SendMoveAction(heldObject.transform.position);
                
                heldObject = null;
            }
        }

        private void OnCutPerformed(InputAction.CallbackContext context)
        {
            if (targetObject != null && targetObject.CanBeCut())
            {
                Vector3 cutPosition = targetObject.transform.position;
                targetObject.OnCut(cutPosition);
                
                // Send cut action to network
                NetworkManager.Instance?.SendCutAction(targetObject.ObjectId, cutPosition);
            }
        }

        private void OnBreakPerformed(InputAction.CallbackContext context)
        {
            if (targetObject != null && targetObject.CanBeBroken())
            {
                string objectId = targetObject.ObjectId;
                targetObject.OnBreak();
                
                // Send break action to network
                NetworkManager.Instance?.SendBreakAction(objectId);
                
                targetObject = null;
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
