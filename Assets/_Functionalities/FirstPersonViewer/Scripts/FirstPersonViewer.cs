using Netherlands3D.Coordinates;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Samplers;
using System;
using System.Collections.Generic;
using Netherlands3D.Twin;
using UnityEngine;
using UnityEngine.Events;

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
        private Action<Vector3, bool> groundCallback;

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

        private MovementVisualController viewObject;

        //Events
        public UnityEvent OnResetToStart = new();
        public UnityEvent OnResetToGround = new();
        public UnityEvent OnSetCameraNorth = new();
        public UnityEvent<Coordinate> OnPositionUpdated = new();

        public UnityEvent OnViewerEntered;
        public UnityEvent<bool> OnViewerExited = new();

        private void Awake()
        {
            Input = GetComponent<FirstPersonViewerInput>();
            MovementSwitcher = GetComponent<MovementModusSwitcher>();

            worldTransform = GetComponent<WorldTransform>();

            Input.SetExitCallback(ExitViewer);
        }

        private void Start()
        {
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();
            MovementSwitcher.SetViewerInput(Input);
            MovementSwitcher.OnMovementPresetChanged += SetMovementModus;

            FirstPersonCamera.onSetupComplete.AddListener(OnAnimationCompleted);

            SetupFSM();
            gameObject.SetActive(false);

            groundCallback = UpdateGroundPosition;
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.position = position;

            Vector3 euler = rotation.eulerAngles;

            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            FirstPersonCamera.transform.localRotation = Quaternion.Euler(euler.x, 0f, 0f);
        }

        public void EnterViewer(ViewerState startState, Dictionary<string, object> settings)
        {
            //Catch Postion picker double enter call (When FPS is low).
            if (FirstPersonCamera.FPVCamera.gameObject.activeInHierarchy && startState == null) return;

            startPosition = new Coordinate(transform.position);
            startRotation = transform.rotation;
            yPositionTarget = transform.position.y;

            worldTransform.MoveToCoordinate(startPosition);
            worldTransform.SetRotation(startRotation);

            //Remove old visual (So no weird transition will happen)
            fsm.SwitchState(null);
            SetMovementVisual(null);

            MovementSwitcher.SetViewer(startState, settings);

            //When entering the FPV for the first time use the cool animation :).
            if (!FirstPersonCamera.FPVCamera.gameObject.activeInHierarchy)
            {
                App.Cameras.SwitchCamera(FirstPersonCamera.FPVCamera);

                bool usePitch = startState != null && startState.CameraConstrain != CameraConstrain.CONTROL_NONE;
                FirstPersonCamera.SetupViewer(usePitch);
                OnViewerEntered.Invoke();
            }
            else if (startState != null) MovementSwitcher.ApplyViewer();
        }

        private void OnAnimationCompleted()
        {
            Input.OnFPVEnter();
            MovementSwitcher.ApplyViewer();
        }

        private void OnDestroy()
        {
            MovementSwitcher.OnMovementPresetChanged -= SetMovementModus;
            FirstPersonCamera.onSetupComplete.RemoveListener(OnAnimationCompleted);
            OnViewerEntered.RemoveAllListeners();
            OnResetToStart.RemoveAllListeners();
            OnResetToGround.RemoveAllListeners();
            OnSetCameraNorth.RemoveAllListeners();
            OnViewerExited.RemoveAllListeners();
        }

        private void SetupFSM()
        {
            ViewerState[] playerStates = MovementSwitcher.MovementPresets.ToArray();

            fsm = new FirstPersonViewerStateMachine(this, Input, playerStates);
        }

        private void Update()
        {
            if (!FirstPersonCamera.FPVCamera.gameObject.activeInHierarchy) return;
            
            CheckGroundCollision();

            fsm.OnUpdate();

            transform.position += Vector3.up * velocity.y * Time.deltaTime;

            OnPositionUpdated.Invoke(new Coordinate(transform.position));

            if (Input.ResetInput.triggered) ResetToGround();
        }

        public void GetGroundPosition()
        {
            raycaster.GetWorldPointFromDirectionAsync(transform.position + Vector3.up * stepHeight, Vector3.down, groundCallback, snappingCullingMask);
        }

        private void UpdateGroundPosition(Vector3 point, bool hit)
        {
            if (hit) yPositionTarget = point.y;
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
            SetMovementVisual(viewerState.viewPrefab);

            viewerState.movementVisualController = viewObject;

            FirstPersonCamera.SetCameraConstrain(viewerState.CameraConstrain);

            fsm.SwitchState(viewerState);
        }

        private void SetMovementVisual(MovementVisualController visualObject)
        {
            if (viewObject != null) Destroy(viewObject.gameObject);

            if (visualObject != null)
            {
                viewObject = Instantiate(visualObject, transform);
            }
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

            OnResetToStart.Invoke();
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

                    OnResetToGround.Invoke();
                }
            }, snappingCullingMask);
        }

        public void ExitViewer(bool exitOriginalPosition)
        {
            OnViewerExited.Invoke(exitOriginalPosition);

            Input.ViewerExited();

            App.Cameras.SwitchToPreviousCamera();
        }

        public void SetVelocity(Vector2 velocity) => this.velocity = velocity;
    }
}