using Netherlands3D.Coordinates;
using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer
{
    [RequireComponent(typeof(FirstPersonViewerInput))]
    public class FirstPersonViewer : MonoBehaviour
    {
        [Header("Camera")]
        [field: SerializeField] public FirstPersonViewerCamera FirstPersonCamera;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private FirstPersonViewerInput input;
        private FirstPersonViewerStateMachine fsm;

        //Movement
        public MovementPresets MovementModus { private set; get; }
        public float MovementSpeed { private set; get; }
        private Coordinate startPosition;
        private Quaternion startRotation;

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

        [Header("Main Cam")]
        [SerializeField] private float cameraHeightAboveGround;
        private Camera mainCam;

        //Previouse Main Camera Values
        private Vector3 prevCameraPosition;
        private Quaternion prevCameraRotation;
        private int prevCameraCullingMask;

        private void Awake()
        {
            input = GetComponent<FirstPersonViewerInput>();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            SetupFSM();

            ViewerEvents.OnSpeedChanged += SetMovementSpeed;
            ViewerEvents.OnMovementPresetChanged += SetMovementModus;
            ViewerEvents.OnViewerExited += ExitViewer;
            ViewerEvents.OnResetToStart += ResetToStart;
            ViewerEvents.OnResetToGround += ResetToGround;

        }

        private void Start()
        {
            startPosition = new Coordinate(transform.position);
            startRotation = transform.rotation;
            yPositionTarget = transform.position.y;

            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings") | (1 << LayerMask.NameToLayer("Default")));

            SetupMainCam();
        }

        private void OnDestroy()
        {
            ViewerEvents.OnSpeedChanged -= SetMovementSpeed;
            ViewerEvents.OnMovementPresetChanged -= SetMovementModus;
            ViewerEvents.OnViewerExited -= ExitViewer;
            ViewerEvents.OnResetToStart -= ResetToStart;
            ViewerEvents.OnResetToGround -= ResetToGround;
        }

        private void SetupFSM()
        {
            ViewerState[] playerStates = GetComponents<ViewerState>();

            fsm = new FirstPersonViewerStateMachine(this, input, null, playerStates);
        }

        //Disable the Main Camera through rendering.
        private void SetupMainCam()
        {
            mainCam = Camera.main;

            prevCameraPosition = mainCam.transform.position;
            prevCameraRotation = mainCam.transform.rotation;
            prevCameraCullingMask = mainCam.cullingMask;

            mainCam.transform.position = transform.position + Vector3.up * 20;
            mainCam.transform.rotation = Quaternion.Euler(90, 0, 0);
            mainCam.cullingMask = 0;

            mainCam.orthographic = true;
            mainCam.targetDisplay = 1;
        }

        private void Update()
        {

            CheckGroundCollision();

            fsm.OnUpdate();

            transform.position += Vector3.up * velocity.y * Time.deltaTime;

            //Update Main Cam position
            Vector3 camPos = transform.position;
            camPos.y = cameraHeightAboveGround;
            mainCam.transform.position = camPos;

            if (input.ResetInput.triggered) ViewerEvents.OnResetToGround?.Invoke();
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

            if (movementPresets.viewMesh != null)
            {
                meshFilter.mesh = movementPresets.viewMesh;
                meshRenderer.materials = movementPresets.meshMaterials;
            }
            else meshFilter.mesh = null;
            

            fsm.SwitchState(movementPresets.GetViewerState());
        }

        private void SetMovementSpeed(float speed) => MovementSpeed = speed / 3.6f;

        private void ResetToStart()
        {
            transform.position = startPosition.ToUnity();
            transform.rotation = startRotation;
            yPositionTarget = transform.position.y;

            //When in the flying state add 1.5 meter offset
            //$$ Should prob check if the Camera Control is Both instead of checking the state (Not extendable) Date: 30-09-2025
            if (fsm.CurrentState.GetType() == typeof(ViewerFlyingState)) transform.position += Vector3.up * 1.5f;  
        }

        //Only way to block input and not include checks in every state.
        public Vector2 GetMoveInput()
        {
            if (input.LockInput) return Vector2.zero;
            else return input.MoveAction.ReadValue<Vector2>();
        }

        private void ResetToGround()
        {
            raycaster.GetWorldPointFromDirectionAsync(transform.position + Vector3.up * cameraHeightAboveGround, Vector3.down, (point, hit) =>
            {
                if (hit)
                {
                    SetVelocity(Vector2.zero);
                    yPositionTarget = point.y;
                    transform.position = new Vector3(transform.position.x, yPositionTarget, transform.position.z);
                }
            }, snappingCullingMask);
        }

        private void ExitViewer()
        {
            mainCam.transform.position = prevCameraPosition;
            mainCam.transform.rotation = prevCameraRotation;
            mainCam.cullingMask = prevCameraCullingMask;
            mainCam.orthographic = false;
            mainCam.targetDisplay = 0;
        }
    }
}