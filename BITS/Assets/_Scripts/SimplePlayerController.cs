using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame
{
    /// <summary>
    /// Simple player controller for testing without network
    /// Just attach this to a GameObject and use WASD to move!
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class SimplePlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 2f;
        [SerializeField] private LayerMask interactableLayer = -1;

        private Rigidbody2D rb;
        private GameObject targetObject;
        private GameObject heldObject;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
        }

        private void Update()
        {
            // Check for nearby interactables
            CheckForInteractables();

            // Handle input
            HandleInput();
        }

        private void FixedUpdate()
        {
            // Get input using new Input System
            Vector2 moveInput = Vector2.zero;
            
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.y += 1;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.y -= 1;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1;
            }

            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector2 movement = new Vector2(moveInput.x, moveInput.y).normalized;
                
                // Move
                rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);

                // Rotate towards movement direction
                if (movement != Vector2.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(movement);
                    //rb.rotation = Quaternion.RotateTowards(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                }
            }
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // E key - Grab/Release
            if (keyboard.eKey.wasPressedThisFrame)
            {
                if (heldObject != null)
                {
                    ReleaseObject();
                }
                else if (targetObject != null)
                {
                    GrabObject(targetObject);
                }
            }

            // C key - Cut
            if (keyboard.cKey.wasPressedThisFrame)
            {
                if (targetObject != null)
                {
                    var simpleInt = targetObject.GetComponent<SimpleInteractable>();
                    var complexInt = targetObject.GetComponent<InteractableObject>();
                    
                    if (simpleInt != null && simpleInt.CanBeCut())
                    {
                        simpleInt.OnCut();
                    }
                    else if (complexInt != null && complexInt.CanBeCut())
                    {
                        complexInt.OnCut(targetObject.transform.position);
                    }
                }
            }

            // B key - Break
            if (keyboard.bKey.wasPressedThisFrame)
            {
                if (targetObject != null)
                {
                    var simpleInt = targetObject.GetComponent<SimpleInteractable>();
                    var complexInt = targetObject.GetComponent<InteractableObject>();
                    
                    if (simpleInt != null && simpleInt.CanBeBroken())
                    {
                        simpleInt.OnBreak();
                        targetObject = null;
                    }
                    else if (complexInt != null && complexInt.CanBeBroken())
                    {
                        complexInt.OnBreak();
                        targetObject = null;
                    }
                }
            }
        }

        private void CheckForInteractables()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange, interactableLayer);
            
            GameObject closest = null;
            float closestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                // Check for SimpleInteractable first, then InteractableObject
                var simpleInteractable = collider.GetComponent<SimpleInteractable>();
                var interactable = collider.GetComponent<InteractableObject>();
                
                if ((simpleInteractable != null || interactable != null) && collider.gameObject != heldObject)
                {
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closest = collider.gameObject;
                        closestDistance = distance;
                    }
                }
            }

            // Update target highlight
            if (targetObject != closest)
            {
                if (targetObject != null)
                {
                    var simpleInt = targetObject.GetComponent<SimpleInteractable>();
                    var complexInt = targetObject.GetComponent<InteractableObject>();
                    if (simpleInt != null) simpleInt.SetHighlight(false);
                    else if (complexInt != null) complexInt.SetHighlight(false);
                }
                
                targetObject = closest;
                
                if (targetObject != null)
                {
                    var simpleInt = targetObject.GetComponent<SimpleInteractable>();
                    var complexInt = targetObject.GetComponent<InteractableObject>();
                    if (simpleInt != null) simpleInt.SetHighlight(true);
                    else if (complexInt != null) complexInt.SetHighlight(true);
                }
            }
        }

        private void GrabObject(GameObject obj)
        {
            var simpleInt = obj.GetComponent<SimpleInteractable>();
            var complexInt = obj.GetComponent<InteractableObject>();
            
            bool canGrab = (simpleInt != null && simpleInt.CanBeGrabbed()) || 
                          (complexInt != null && complexInt.CanBeGrabbed());
            
            if (canGrab)
            {
                heldObject = obj;
                
                if (simpleInt != null) simpleInt.OnGrabbed();
                else if (complexInt != null) complexInt.OnGrabbed("local_player");
                
                // Attach object to player
                obj.transform.SetParent(transform);
                obj.transform.localPosition = new Vector3(0, 1, 1);
                
                Debug.Log($"Grabbed {obj.name}");
            }
        }

        private void ReleaseObject()
        {
            if (heldObject != null)
            {
                Debug.Log($"Released {heldObject.name}");
                
                var simpleInt = heldObject.GetComponent<SimpleInteractable>();
                var complexInt = heldObject.GetComponent<InteractableObject>();
                
                if (simpleInt != null) simpleInt.OnReleased();
                else if (complexInt != null) complexInt.OnReleased();
                
                heldObject.transform.SetParent(null);
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
