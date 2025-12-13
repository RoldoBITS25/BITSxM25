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

        private void Update()
        {
            if (!isLocalPlayer)
                return;

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
                NetworkManager.Instance?.SendMoveAction(transform.position);
            }
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
                rb.MovePosition(rb.position + movement);
            }
        }
    }
}
