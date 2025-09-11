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
        [Header("State Machine")]
        private FirstPersonViewerStateMachine fsm;
        [SerializeField] private ViewerState startState; //Should be from default MovementPreset.

        [Header("Input")]
        [SerializeField] private InputActionAsset inputActionAsset;

        public InputAction MoveAction { private set; get; }
        public InputAction SprintAction {  private set; get; }

        [Header("Movement")]
        [field: SerializeField, Tooltip("temporary movement input (Should be replaced with button event or something like that)")] public MovementPresets MovementModus { private set; get; }
        public float MovementSpeed { private set; get; }

        //Raycasting
        private OpticalRaycaster raycaster;
        private int snappingCullingMask = 0;

        //Falling
        public Vector2 Velocity => velocity;
        private Vector2 velocity;
        private readonly float gravity = -9.81f;
        private readonly float maxFallSpeed = -55f;
        private float yPositionTarget;
        public bool isGrounded;
        [SerializeField] private float groundDistance = .2f;

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
            MovementSpeed = MovementModus.speedInKm / 3.6f;

            yPositionTarget = transform.position.y;

            MoveAction = inputActionAsset.FindAction("Move");
            SprintAction = inputActionAsset.FindAction("Sprint");

            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings") | (1 << LayerMask.NameToLayer("Default")));

            SetupFSM();
        }

        private void SetupFSM()
        {
            ViewerState[] playerStates = GetComponents<ViewerState>();

            fsm = new FirstPersonViewerStateMachine(this, startState.GetType(), playerStates);
        }

        private void Update()
        {
            CheckGroundCollision();

            fsm.OnUpdate();

            transform.position += Vector3.up * velocity.y * Time.deltaTime;
        }

        public void GetGroundPosition()
        {
            raycaster.GetWorldPointFromDirectionAsync(transform.position + Vector3.up * MovementModus.stepHeight, Vector3.down, (point, hit) =>
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

            if (Mathf.Abs(transform.position.y - yPositionTarget) < groundDistance) isGrounded = true;
        }

        public void ApplyGravity()
        {
            if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
                velocity.y = Mathf.Max(velocity.y, maxFallSpeed);
            }
        }

        public void SetVelocity(Vector2 velocity) => this.velocity = velocity;
    }
}