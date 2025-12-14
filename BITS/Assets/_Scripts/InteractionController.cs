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
        private PlayerController playerController;
        
        // Public property to check if holding an object
        public bool IsHoldingObject => heldObject != null;

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
            playerController = GetComponent<PlayerController>();
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
//                 Debug.Log("Interacting");
                CheckForInteractables();
            }

        }


        private void CheckForInteractables()
        {
             // Get the nearest game object within interaction range
            GameObject nearestObject = GetNearestGameObject(interactionRange);
            
            Debug.Log($"[InteractionController] E pressed! Nearest object: {(nearestObject != null ? nearestObject.name : "NONE")}");
            
            if (nearestObject == null)
            {
                Debug.Log("[InteractionController] No object in range!");
                return;
            }
            
            // Check for TextDisplayObject first (works with any weapon)
            var textDisplay = nearestObject.GetComponent<TextDisplayObject>();
            if (textDisplay != null)
            {
                textDisplay.OnInteract(playerId);
                return;
            }
            
            // Check for InputTextObject (works with any weapon)
            var inputTextObject = nearestObject.GetComponent<InputTextObject>();
            if (inputTextObject != null)
            {
                inputTextObject.OnInteract(playerId);
                return;
            }
            
            // Interaction logic dependent on weapon
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogWarning("[InteractionController] No PlayerController found!");
                return;
            }
            
            var weapon = playerController.CurrentWeapon;
            Debug.Log($"[InteractionController] Current weapon: {weapon}");
            
            // If holding an object, only allow releasing it
            if (heldObject != null)
            {
                if (weapon == WeaponType.Paper)
                {
                    Debug.Log("[InteractionController] Releasing held object");
                    ReleaseObject();
                }
                else
                {
                    Debug.Log("[InteractionController] Cannot interact while holding an object! Switch to Paper to release.");
                }
                return;
            }
            
            if (weapon == WeaponType.Paper)
            {
                 Debug.Log("[InteractionController] Paper weapon - attempting to grab");
                 // Grab - only if no object is currently held and object is grabbable
                 var grabbable = nearestObject.GetComponent<IGrabbable>();
                 if (grabbable != null)
                 {
                     Debug.Log($"[InteractionController] Found IGrabbable, CanBeGrabbed: {grabbable.CanBeGrabbed()}");
                     if (grabbable.CanBeGrabbed())
                     {
                         GrabObject(grabbable);
                     }
                     else
                     {
                         Debug.Log("[InteractionController] Object cannot be grabbed (already grabbed or disabled)");
                     }
                 }
                 else
                 {
                     Debug.Log($"[InteractionController] No IGrabbable component found on {nearestObject.name}");
                 }
            }
            else if (weapon == WeaponType.Scissors)
            {
                 Debug.Log("[InteractionController] Scissors weapon - attempting to cut");
                 // Cut - only if object is cuttable
                 // Try ICuttable first
                 var cuttable = nearestObject.GetComponent<ICuttable>();
                 if (cuttable != null)
                 {
                     Debug.Log($"[InteractionController] Found ICuttable on {nearestObject.name}, CanBeCut: {cuttable.CanBeCut()}");
                     if (cuttable.CanBeCut())
                     {
                         Debug.Log("[InteractionController] Cutting object!");
                         cuttable.OnCut(transform.position);
                         string id = GetObjectId(nearestObject);
                         if (id != null) NetworkManager.Instance?.SendCutAction(id, transform.position);
                     }
                 }
                 else
                 {
                     Debug.Log($"[InteractionController] No ICuttable found on {nearestObject.name}");
                     // Fallback to InteractableObject
                     var io = nearestObject.GetComponent<InteractableObject>();
                     if (io != null && io.CanBeCut())
                     {
                         io.OnCut(transform.position);
                         NetworkManager.Instance?.SendCutAction(io.ObjectId, transform.position);
                     }
                 }
            }
            else if (weapon == WeaponType.Rock)
            {
                 Debug.Log("[InteractionController] Rock weapon - attempting to break");
                 
                 // Debug: List all components on the object
                 var allComponents = nearestObject.GetComponents<Component>();
                 Debug.Log($"[InteractionController] Object '{nearestObject.name}' has {allComponents.Length} components:");
                 foreach (var comp in allComponents)
                 {
                     if (comp != null)
                     {
                         Debug.Log($"  - {comp.GetType().Name} (Full: {comp.GetType().FullName})");
                     }
                 }
                 
                 // Break - only if object is breakable
                 var breakable = nearestObject.GetComponent<IBreakable>();
                 if (breakable != null)
                 {
                      Debug.Log($"[InteractionController] Found IBreakable on {nearestObject.name}, CanBeBroken: {breakable.CanBeBroken()}");
                      if (breakable.CanBeBroken())
                      {
                          Debug.Log("[InteractionController] Breaking object!");
                          breakable.OnBreak();
                          string id = GetObjectId(nearestObject);
                          if (id != null) NetworkManager.Instance?.SendBreakAction(id);
                      }
                 }
                 else
                 {
                     Debug.Log($"[InteractionController] No IBreakable found on {nearestObject.name}");
                     // Fallback to InteractableObject
                     var io = nearestObject.GetComponent<InteractableObject>();
                     if (io != null && io.CanBeBroken())
                     {
                         io.OnBreak();
                         NetworkManager.Instance?.SendBreakAction(io.ObjectId);
                     }
                 }
            }
        }

        private string GetObjectId(GameObject obj)
        {
            var io = obj.GetComponent<InteractableObject>();
            if (io != null) return io.ObjectId;
            var bo = obj.GetComponent<BreakableObject>();
            if (bo != null) return bo.GetObjectId();
            return null;
        }


        private void GrabObject(IGrabbable obj)
        {
            if (obj.CanBeGrabbed())
            {
                heldObject = obj;
                obj.OnGrabbed(playerId);
                
                // Send grab action to network
                string id = GetObjectId(obj.GetTransform().gameObject);
                if (id != null) NetworkManager.Instance?.SendGrabAction(id);
                
                // Attach object to player
                Transform objTransform = obj.GetTransform();
                objTransform.SetParent(transform);
                
                // Position on top of player - check if object has custom grab offset
                Vector3 offset = new Vector3(0, 2f, 0); // Default: 2 units above player
                
                // Try to get custom offset from MovableObject
                var movable = objTransform.GetComponent<MovableObject>();
                if (movable != null)
                {
                    offset = movable.GetGrabOffset();
                }
                
                objTransform.localPosition = offset;
                
                Debug.Log($"[InteractionController] Grabbed object positioned at local offset: {offset}");
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
