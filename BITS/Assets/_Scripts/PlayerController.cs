using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame
{
    /// <summary>
    /// Controls player movement
    /// Handles movement input and sends position updates to the network
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        private string playerId;
        private bool isLocalPlayer;
        private Rigidbody rb;
        private Vector3 moveInput;

        // Input Actions
        private PlayerInput playerInput;
        private InputAction moveAction;

        public void Initialize(string playerId, bool isLocal)
        {
            this.playerId = playerId;
            this.isLocalPlayer = isLocal;

            // if (!isLocal)
            // {
            //     // Disable input for remote players
            //     if (playerInput != null)
            //         playerInput.enabled = false;
            // }
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;

            // Get Input System component
            playerInput = GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                try
                {
                    moveAction = playerInput.actions["Move"];
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[PlayerController] Move action not found: {ex.Message}. Using fallback input.");
                }
            }
        }




        // Weapon System
        public WeaponType CurrentWeapon { get; set; } = WeaponType.None;

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            // Weapon Swap Input
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame)
                {
                    AttemptSwapWeapon(WeaponType.Paper);
                }
                else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                {
                    AttemptSwapWeapon(WeaponType.Scissors);
                }
                else if (Keyboard.current.digit3Key.wasPressedThisFrame)
                {
                    AttemptSwapWeapon(WeaponType.Rock);
                }
            }

            // Read movement input
            Vector2 input = Vector2.zero;
            
            // Try reading from configured action
            if (moveAction != null)
            {
                input = moveAction.ReadValue<Vector2>();
            }
            
            // Fallback to direct keyboard polling if no input detected or action missing
            if (input == Vector2.zero)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1;
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1;
                }
            }

            if (input != Vector2.zero)
            {
                Debug.Log($"[PlayerController] Input detected: {input}");
            }

            // Map 2D input (x,y) to 3D movement (x,0,z)
            moveInput = new Vector3(input.x, 0f, input.y);

            // Send position updates periodically
            if (Time.frameCount % 10 == 0) // Every 10 frames
            {
                if (NetworkManager.Instance != null)
                {
                    Debug.Log($"[PlayerController] Sending position update: {transform.position}");
                    NetworkManager.Instance.SendMoveAction(transform.position);
                }
                else
                {
                    Debug.LogWarning("[PlayerController] NetworkManager.Instance is null, cannot send move action");
                }
            }
        }

        private void AttemptSwapWeapon(WeaponType newWeapon)
        {
            if (CurrentWeapon == newWeapon) return;

            // Check if weapon is already taken by another player
            if (GameStateManager.Instance != null && GameStateManager.Instance.IsWeaponTaken(newWeapon))
            {
                Debug.LogWarning($"[PlayerController] Cannot swap to {newWeapon} - taken by another player!");
                return;
            }

            Debug.Log($"[PlayerController] Swapping to {newWeapon}...");
            
            // Optimistically update
            CurrentWeapon = newWeapon;
            
            // Visual feedback (Todo: proper specific visual)
            Debug.Log($"[PlayerController] Current Weapon: {CurrentWeapon}");
            
            // Send to network
            NetworkManager.Instance?.SendSwapWeaponAction(newWeapon.ToString().ToLower());
        }

        private void FixedUpdate()
        {
            if (!isLocalPlayer)
                return;

            // Handle 3D movement
            if (moveInput.sqrMagnitude > 0.01f)
            {
                // Move in 3D space (X, Z plane)
                Vector3 movement = moveInput.normalized * moveSpeed * Time.fixedDeltaTime;
                Vector3 newPosition = rb.position + movement;
                Debug.Log($"[PlayerController] FixedUpdate - Moving from {rb.position} to {newPosition}, moveInput: {moveInput}");
                rb.MovePosition(newPosition);
            }
        }
    }
}
