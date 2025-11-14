using Netherlands3D.Coordinates;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Samplers;
using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer
{
    [RequireComponent(typeof(FirstPersonViewerInput), typeof(MovementModusSwitcher))]
    public class FirstPersonViewer : MonoBehaviour
    {
        [Header("Camera")]
        [field: SerializeField] public FirstPersonViewerCamera FirstPersonCamera { private set; get; }
        public FirstPersonViewerInput Input { private set; get; }
        public MovementModusSwitcher MovementSwitcher { private set; get; }

        private FirstPersonViewerStateMachine fsm;
        private WorldTransform worldTransform;

        //Movement
        private Coordinate startPosition;
        private Quaternion startRotation;

        [Header("Raycasting")]
        [SerializeField] private LayerMask snappingCullingMask;
        private OpticalRaycaster raycaster;

        //Falling
        [Header("Ground")]
        [SerializeField] private float groundDistance = .2f;
        public Vector2 Velocity => velocity;
        private Vector2 velocity;
        private readonly float gravity = -9.81f;
        private readonly float maxFallSpeed = -55f;
        private float yPositionTarget;
        public bool isGrounded;

        [Header("Settings")]
        [SerializeField] private float stepHeight = 1.5f;
        [SerializeField] private float returnFocusDistance = 150;

        private GameObject viewObject;

        //Events
        public Action OnResetToStart;
        public Action OnResetToGround;
        public Action OnSetCameraNorth;

        public Action OnViewerEntered;
        public Action<bool> OnViewerExited;


        private void Awake()
        {
            Input = GetComponent<FirstPersonViewerInput>();
            MovementSwitcher = GetComponent<MovementModusSwitcher>();

            worldTransform = GetComponent<WorldTransform>();

            OnViewerEntered += ViewerEnterd;
            Input.SetExitCallback(ExitViewer);
        }

        private void Start()
        {
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();
            MovementSwitcher.SetViewerInput(Input);
            MovementSwitcher.OnMovementPresetChanged += SetMovementModus;

            SetupFSM();
            gameObject.SetActive(false);
        }

        private void ViewerEnterd()
        {
            startPosition = new Coordinate(transform.position);
            startRotation = transform.rotation;
            yPositionTarget = transform.position.y;

            worldTransform.MoveToCoordinate(startPosition);
            Input.OnFPVEnter();
            FirstPersonCamera.SetupViewer();

            ServiceLocator.GetService<CameraSwitcher>().SwitchCamera(this);
        }

        private void OnDestroy()
        {
            MovementSwitcher.OnMovementPresetChanged -= SetMovementModus;
            OnViewerEntered = null;
            OnResetToStart = null;
            OnResetToGround = null;
            OnSetCameraNorth = null;
            OnViewerEntered = null;
            OnViewerExited = null;
        }

        private void SetupFSM()
        {
            ViewerState[] playerStates = MovementSwitcher.MovementPresets.ToArray();

            fsm = new FirstPersonViewerStateMachine(this, Input, playerStates);
        }

        private void Update()
        {
            CheckGroundCollision();

            fsm.OnUpdate();

            transform.position += Vector3.up * velocity.y * Time.deltaTime;

            if (Input.ResetInput.triggered) ResetToGround();
        }

        public void GetGroundPosition()
        {
            raycaster.GetWorldPointFromDirectionAsync(transform.position + Vector3.up * stepHeight, Vector3.down, (point, hit) =>
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

        private void SetMovementModus(ViewerState viewerState)
        {
            if (viewObject != null)
            {
                Destroy(viewObject);
            }

            if (viewerState.viewPrefab != null)
            {
                viewObject = Instantiate(viewerState.viewPrefab, transform);
                MovementVisualController movementVisualController = viewObject.GetComponent<MovementVisualController>();
                viewerState.movementVisualController = movementVisualController;
            }

            FirstPersonCamera.SetCameraConstrain(viewerState.CameraConstrain);

            fsm.SwitchState(viewerState);
        }

        public void SetupState(Vector3 cameraPosition, Vector3 playerEuler, Vector3 cameraEuler, float cameraHeightOffset)
        {
            transform.position = cameraPosition + Vector3.down * cameraHeightOffset;
            FirstPersonCamera.transform.localPosition = Vector3.up * cameraHeightOffset;

            transform.rotation = Quaternion.Euler(playerEuler);
            FirstPersonCamera.transform.localRotation = Quaternion.Euler(cameraEuler);
        }

        public void ResetToStart()
        {
            transform.position = startPosition.ToUnity();
            transform.rotation = startRotation;
            yPositionTarget = transform.position.y;

            transform.position += Vector3.up * fsm.CurrentState.GetGroundHeightOffset();

            OnResetToStart?.Invoke();
        }

        public void ResetToGround()
        {
            raycaster.GetWorldPointFromDirectionAsync(transform.position + Vector3.up * 100, Vector3.down, (point, hit) =>
            {
                if (hit)
                {
                    SetVelocity(Vector2.zero);
                    yPositionTarget = point.y;
                    transform.position = new Vector3(transform.position.x, yPositionTarget + fsm.CurrentState.GetGroundHeightOffset(), transform.position.z);

                    OnResetToGround?.Invoke();
                }
            }, snappingCullingMask);
        }

        //Bool is needed for another branch to prevent another scene update in the future (The branch where it's used is already ready) | WHEN USED PLEASE REMOVE COMMENT :)
        public void ExitViewer(bool exitOriginalPosition)
        {
            OnViewerExited?.Invoke(exitOriginalPosition);

            Input.ViewerExited();

            ServiceLocator.GetService<CameraSwitcher>().SwitchToPreviousCamera();
        }

        public void SetVelocity(Vector2 velocity) => this.velocity = velocity;
    }
}