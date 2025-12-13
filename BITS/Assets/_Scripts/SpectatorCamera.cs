using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiplayerGame
{
    /// <summary>
    /// Spectator camera controller
    /// Allows free movement to observe the game
    /// </summary>
    public class SpectatorCamera : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float fastMoveSpeed = 20f;
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float zoomSpeed = 5f;

        [Header("Follow Settings")]
        [SerializeField] private bool followPlayer = false;
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 followOffset = new Vector3(0, 5, -5);

        private Camera cam;
        private Vector2 lookInput;
        private Vector2 moveInput;
        private float zoomInput;
        private bool isFastMove;

        private PlayerInput playerInput;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction zoomAction;
        private InputAction fastMoveAction;
        private InputAction toggleFollowAction;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = GetComponentInChildren<Camera>();
            }

            // Setup Input System
            playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                moveAction = playerInput.actions["Move"];
                lookAction = playerInput.actions["Look"];
                zoomAction = playerInput.actions["Zoom"];
                fastMoveAction = playerInput.actions["FastMove"];
                toggleFollowAction = playerInput.actions["ToggleFollow"];
            }
        }

        private void OnEnable()
        {
            if (fastMoveAction != null)
            {
                fastMoveAction.performed += ctx => isFastMove = true;
                fastMoveAction.canceled += ctx => isFastMove = false;
            }

            if (toggleFollowAction != null)
            {
                toggleFollowAction.performed += OnToggleFollow;
            }
        }

        private void OnDisable()
        {
            if (toggleFollowAction != null)
            {
                toggleFollowAction.performed -= OnToggleFollow;
            }
        }

        private void Update()
        {
            // Read inputs
            if (moveAction != null)
                moveInput = moveAction.ReadValue<Vector2>();

            if (lookAction != null)
                lookInput = lookAction.ReadValue<Vector2>();

            if (zoomAction != null)
                zoomInput = zoomAction.ReadValue<float>();

            if (followPlayer && followTarget != null)
            {
                FollowTargetPlayer();
            }
            else
            {
                FreeCameraMovement();
            }
        }

        private void FreeCameraMovement()
        {
            // Movement
            float currentSpeed = isFastMove ? fastMoveSpeed : moveSpeed;
            Vector3 movement = transform.right * moveInput.x + transform.forward * moveInput.y;
            transform.position += movement * currentSpeed * Time.deltaTime;

            // Rotation (mouse look)
            if (lookInput.sqrMagnitude > 0.01f)
            {
                float yaw = lookInput.x * rotationSpeed;
                float pitch = -lookInput.y * rotationSpeed;

                transform.Rotate(Vector3.up, yaw, Space.World);
                transform.Rotate(Vector3.right, pitch, Space.Self);

                // Clamp pitch
                Vector3 euler = transform.eulerAngles;
                if (euler.x > 180)
                    euler.x -= 360;
                euler.x = Mathf.Clamp(euler.x, -80, 80);
                transform.eulerAngles = euler;
            }

            // Zoom (adjust FOV)
            if (cam != null && Mathf.Abs(zoomInput) > 0.01f)
            {
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView - zoomInput * zoomSpeed * Time.deltaTime, 30, 90);
            }
        }

        private void FollowTargetPlayer()
        {
            if (followTarget == null)
            {
                followPlayer = false;
                return;
            }

            // Smooth follow
            Vector3 targetPosition = followTarget.position + followOffset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 5f);

            // Look at target
            transform.LookAt(followTarget);
        }

        private void OnToggleFollow(InputAction.CallbackContext context)
        {
            followPlayer = !followPlayer;

            if (followPlayer)
            {
                // Find a player to follow
                FindPlayerToFollow();
            }
        }

        private void FindPlayerToFollow()
        {
            // Find the first player in the scene
            var players = FindObjectsOfType<PlayerController>();
            if (players.Length > 0)
            {
                followTarget = players[0].transform;
            }
            else
            {
                followPlayer = false;
            }
        }

        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            followPlayer = target != null;
        }
    }
}
