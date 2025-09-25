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
        private FirstPersonViewerInput input;
        private FirstPersonViewerStateMachine fsm;

        //Movement
        public MovementPresets MovementModus { private set; get; }
        public float MovementSpeed { private set; get; }
        private Coordinate startPosition;
        private Quaternion startRotation;

        //Raycasting
        //private OpticalInstantRaycaster test;
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
        private Vector3 prevCameraPosition;
        private Quaternion prevCameraRotation;
        private int prevCameraCullingMask;

        private void Awake()
        {
            input = GetComponent<FirstPersonViewerInput>();
            meshFilter = GetComponent<MeshFilter>();

            SetupFSM();

            ViewerEvents.OnSpeedChanged += SetMovementSpeed;
            ViewerEvents.OnMovementPresetChanged += SetMovementModus;
            ViewerEvents.OnViewerExited += ExitViewer;
            ViewerEvents.OnResetToStart += ResetToStart;
        }

        private void Start()
        {
            startPosition = new Coordinate(transform.position);
            startRotation = transform.rotation;
            yPositionTarget = transform.position.y;
            
            //test = ServiceLocator.GetService<OpticalInstantRaycaster>();
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings") | (1 << LayerMask.NameToLayer("Default")));

            SetupMainCam();
        }

        private void OnDestroy()
        {
            ViewerEvents.OnSpeedChanged -= SetMovementSpeed;
            ViewerEvents.OnMovementPresetChanged -= SetMovementModus;
            ViewerEvents.OnViewerExited -= ExitViewer;
            ViewerEvents.OnResetToStart -= ResetToStart;
        }

        private void SetupFSM()
        {
            ViewerState[] playerStates = GetComponents<ViewerState>();

            fsm = new FirstPersonViewerStateMachine(this, input, null, playerStates);
        }

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
            Vector3 camPos = transform.position; //CHECK
            camPos.y = cameraHeightAboveGround;
            mainCam.transform.position = camPos;
        }

        public void GetGroundPosition()
        {
            //Vector3 point = test.GetWorldPointFromPosition(transform.position + Vector3.up * MovementModus.stepHeight * 5f, Vector3.down);
            //Debug.Log(point);
            //yPositionTarget = point.y;

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

            fsm.SwitchState(movementPresets.GetViewerState());
        }

        private void SetMovementSpeed(float speed) => MovementSpeed = speed / 3.6f;

        private void ResetToStart()
        {
            transform.position = startPosition.ToUnity();
            transform.rotation = startRotation;
            yPositionTarget = transform.position.y;

            //When in the flying state add 1.5 meter offset
            if (fsm.CurrentState.GetType() == typeof(ViewerFlyingState)) transform.position += Vector3.up * 1.5f;  
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