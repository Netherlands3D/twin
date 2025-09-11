using DG.Tweening;
using Netherlands3D.Events;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer
{
    public class FirstPersonViewer : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActionAsset;

        private InputAction moveAction;
        private InputAction sprintAction;

        [Header("Movement"), Tooltip("temporary movement input (Should be replaced with button event or something like that)")]
        [SerializeField] private MovementPresets movementModus;
        private float movementSpeed;

        //Raycasting
        private OpticalRaycaster raycaster;
        private int snappingCullingMask = 0;

        //Falling
        private Vector2 velocity;
        private readonly float gravity = -9.81f;
        private readonly float maxFallSpeed = -55f;
        private float yPositionTarget;
        private bool isGrounded;

        private void OnEnable()
        {
            inputActionAsset.Enable();
        }

        private void OnDisable()
        {
            inputActionAsset.Disable();
        }

        private void Start()
        {
            //TEMP
            movementSpeed = movementModus.speedInKm / 3.6f;

            yPositionTarget = transform.position.y;

            moveAction = inputActionAsset.FindAction("Move");
            sprintAction = inputActionAsset.FindAction("Sprint");

            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings") | (1 << LayerMask.NameToLayer("Default")));
        }

        private void Update()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            if(moveInput.magnitude > 0)
            {
                MovePlayer(moveInput);
            }

            CheckGroundCollision();

            Jump();

            ApplyGravity();

            transform.position += Vector3.up * velocity.y * Time.deltaTime;
        }

        private void MovePlayer(Vector2 moveInput)
        {
            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float calculatedSpeed = movementSpeed * (sprintAction.IsPressed() ? movementModus.runningMultiplier : 1);

            transform.Translate(direction * calculatedSpeed * Time.deltaTime, Space.World);
            GetGroundPosition();
        }

        private void GetGroundPosition()
        {
            raycaster.GetWorldPointFromDirectionAsync(transform.position + Vector3.up * movementModus.stepHeight, Vector3.down, (point, hit) =>
            {
                if (hit)
                {
                    yPositionTarget = point.y;
                }
            }, snappingCullingMask);
        }

        private void CheckGroundCollision()
        {
            if (transform.position.y <= yPositionTarget)
            {
                transform.position = new Vector3(transform.position.x, yPositionTarget, transform.position.z);
                velocity.y = Mathf.Max(0, velocity.y);
                isGrounded = true;
            }
            else isGrounded = false;
        }

        private void Jump()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
            {
                velocity.y = movementModus.jumpHeight;
                isGrounded = false;
            }
        }

        private void ApplyGravity()
        {
            if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
                velocity.y = Mathf.Max(velocity.y, maxFallSpeed);
            }
        }

       
    }
}