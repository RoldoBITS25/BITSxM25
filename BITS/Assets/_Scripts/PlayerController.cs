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

        /// <summary>
        /// Sets the weapon and triggers the OnWeaponChange callback
        /// </summary>
        public void SetWeapon(WeaponType weapon)
        {
            if (CurrentWeapon != weapon)
            {
                CurrentWeapon = weapon;
                OnWeaponChange(weapon);
            }
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
                    // Silently fall back to keyboard input
                }
            }
        }




        // Weapon System
        public WeaponType CurrentWeapon { get; set; } = WeaponType.None;

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            // Weapon Swap Input - Space key cycles through weapons
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                WeaponType nextWeapon = GetNextWeapon();
                AttemptSwapWeapon(nextWeapon);
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



            // Map 2D input (x,y) to 3D movement (x,0,z)
            moveInput = new Vector3(input.x, 0f, input.y);

            // Send position updates periodically
            if (Time.frameCount % 10 == 0) // Every 10 frames
            {
                if (NetworkManager.Instance != null)
                {
                    NetworkManager.Instance.SendMoveAction(transform.position);
                }
            }
        }


        private WeaponType GetNextWeapon()
        {
            switch (CurrentWeapon)
            {
                case WeaponType.None:
                    return WeaponType.Paper;
                case WeaponType.Paper:
                    return WeaponType.Scissors;
                case WeaponType.Scissors:
                    return WeaponType.Rock;
                case WeaponType.Rock:
                    return WeaponType.Paper;
                default:
                    return WeaponType.Paper;
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
            
            // Optimistically update
            CurrentWeapon = newWeapon;
            
            // Call weapon change callback
            OnWeaponChange(newWeapon);
            
            // Display current weapon
            Debug.Log($"[PlayerController] ★ CURRENT PLAYER WEAPON: {CurrentWeapon} ★");
            
            // Log all other players' weapons
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.LogAllPlayerWeapons();
            }
            
            // Send to network
            NetworkManager.Instance?.SendSwapWeaponAction(newWeapon.ToString().ToLower());
        }

        /// <summary>
        /// Called whenever the weapon changes
        /// Override or extend this method to add custom behavior on weapon change
        /// </summary>
        protected virtual void OnWeaponChange(WeaponType newWeapon)
        {
            Debug.Log($"[PlayerController] OnWeaponChange called: {newWeapon}");
            // Add custom weapon change logic here
            // e.g., update UI, play sound effects, change visual appearance, etc.
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
                rb.MovePosition(newPosition);
            }
        }
    }
}
