using DG.Tweening;
using Netherlands3D.Events;
using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer
{
    [RequireComponent(typeof(FirstPersonViewerInput))]
    public class FirstPersonViewer : MonoBehaviour
    {
        [Header("Camera")]
        [field: SerializeField] public FirstPersonViewerCamera FirstPersonCamera;

        private MeshFilter meshFilter;
        private FirstPersonViewerInput input;
        private FirstPersonViewerStateMachine fsm;

        //Movement
        public MovementPresets MovementModus { private set; get; }
        public float MovementSpeed { private set; get; }

        //Raycasting
        private OpticalRaycaster raycaster;
        private int snappingCullingMask;

        //Falling
        [Header("Ground")]
        [SerializeField] private float groundDistance = .2f;
        public Vector2 Velocity => velocity;
        private Vector2 velocity;
        private readonly float gravity = -9.81f;
        private readonly float maxFallSpeed = -55f;
        private float yPositionTarget;
        public bool isGrounded;

        private void Awake()
        {
            input = GetComponent<FirstPersonViewerInput>();
            meshFilter = GetComponent<MeshFilter>();

            SetupFSM();

            ViewerEvents.ChangeSpeed += SetMovementSpeed;
            ViewerEvents.OnMovementPresetChanged += SetMovementModus;
        }

        private void Start()
        {
            yPositionTarget = transform.position.y;
            
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings") | (1 << LayerMask.NameToLayer("Default")));
        }

        private void OnDestroy()
        {
            ViewerEvents.ChangeSpeed -= SetMovementSpeed;
            ViewerEvents.OnMovementPresetChanged -= SetMovementModus;
        }

        private void SetupFSM()
        {
            ViewerState[] playerStates = GetComponents<ViewerState>();

            fsm = new FirstPersonViewerStateMachine(this, input, null, playerStates);
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
            if (Mathf.Abs(transform.position.y - yPositionTarget) < groundDistance) isGrounded = true;
            else isGrounded = false;
        }

        public void SnapToGround()
        {
            if (transform.position.y <= yPositionTarget)
            {
                transform.position = new Vector3(transform.position.x, yPositionTarget, transform.position.z);
                velocity.y = Mathf.Max(0, velocity.y);
            }
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

        private void SetMovementModus(MovementPresets movementPresets)
        {
            MovementModus = movementPresets;

            if(movementPresets.viewMesh != null) meshFilter.mesh = movementPresets.viewMesh;
            else meshFilter.mesh = null;

            switch (movementPresets.viewModus)
            {
                case ViewModus.ViewModus.STANDARD:
                    fsm.SwitchState(typeof(ViewerWalkingState));
                    break;
                case ViewModus.ViewModus.VEHICULAR:
                    fsm.SwitchState(typeof(ViewerVehicularState));
                    break;
                case ViewModus.ViewModus.FREECAM:
                    fsm.SwitchState(typeof(ViewerFlyingState));
                    break;
            }
        }


        private void SetMovementSpeed(float speed) => MovementSpeed = speed / 3.6f;
    }
}