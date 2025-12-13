using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame
{
    /// <summary>
    /// Controls player movement
    /// Handles movement input and sends position updates to the network
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        private string playerId;
        private bool isLocalPlayer;
        private Rigidbody2D rb;
        private Vector2 moveInput;

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
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;

            // Get Input System component
            playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                moveAction = playerInput.actions["Move"];
            }
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

            // Handle 2D movement
            if (moveInput.sqrMagnitude > 0.01f)
            {
                // Move in 2D space (X, Y plane)
                Vector2 movement = moveInput.normalized * moveSpeed * Time.fixedDeltaTime;
                rb.MovePosition(rb.position + movement);
            }
        }
    }
}
