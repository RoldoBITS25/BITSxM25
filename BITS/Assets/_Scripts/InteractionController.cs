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
            
            // Interaction logic dependent on weapon
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerController == null) return;
            
            var weapon = playerController.CurrentWeapon;
            
            // Release if holding something and weapon is Paper
            if (weapon == WeaponType.Paper && heldObject != null)
            {
                 ReleaseObject();
                 return;
            }

            if (nearestObject == null) return;
            
            if (weapon == WeaponType.Paper)
            {
                 // Grab
                 var grabbable = nearestObject.GetComponent<IGrabbable>();
                 if (grabbable != null) GrabObject(grabbable);
            }
            else if (weapon == WeaponType.Scissors)
            {
                 // Cut
                 // Try ICuttable first
                 var cuttable = nearestObject.GetComponent<ICuttable>();
                 if (cuttable != null)
                 {
                     if (cuttable.CanBeCut())
                     {
                         cuttable.OnCut(transform.position);
                         string id = GetObjectId(nearestObject);
                         if (id != null) NetworkManager.Instance?.SendCutAction(id, transform.position);
                     }
                 }
                 else 
                 {
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
                 // Break
                 var breakable = nearestObject.GetComponent<IBreakable>();
                 if (breakable != null)
                 {
                      breakable.OnBreak(); // Changed to OnBreak
                      string id = GetObjectId(nearestObject);
                      if (id != null) NetworkManager.Instance?.SendBreakAction(id);
                 }
                 else
                 {
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
