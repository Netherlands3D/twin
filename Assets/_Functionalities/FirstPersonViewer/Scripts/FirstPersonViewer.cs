using Netherlands3D.Coordinates;
using Netherlands3D.Events;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using Netherlands3D.Twin.Samplers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer
{
    [RequireComponent(typeof(FirstPersonViewerInput))]
    public class FirstPersonViewer : MonoBehaviour
    {
        [Header("Camera")]
        [field: SerializeField] public FirstPersonViewerCamera FirstPersonCamera { private set; get; }

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private FirstPersonViewerInput input;
        private FirstPersonViewerStateMachine fsm;
        private MovementModusSwitcher movementSwitcher;

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

        //Events
        public Action OnResetToStart;
        public Action OnResetToGround;
        public Action OnSetCameraNorth;
        public static Action<Vector3> OnCameraRotation; //Temp static because of 


        public static Action OnViewerEntered;
        public static Action OnViewerExited;

        private void Awake()
        {
            input = GetComponent<FirstPersonViewerInput>();
            movementSwitcher = GetComponent<MovementModusSwitcher>();

            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            raycaster = ServiceLocator.GetService<OpticalRaycaster>();

            OnResetToStart += ResetToStart;
            OnResetToGround += ResetToGround;
        }

        private void Start()
        {
            startPosition = new Coordinate(transform.position);
            startRotation = transform.rotation;
            yPositionTarget = transform.position.y;

            ServiceLocator.GetService<MovementModusSwitcher>().OnMovementPresetChanged += SetMovementModus;

            SetupFSM();
        }

        private void OnDestroy()
        {
            ServiceLocator.GetService<MovementModusSwitcher>().OnMovementPresetChanged -= SetMovementModus;
            OnResetToStart -= ResetToStart;
            OnResetToGround -= ResetToGround;
        }

        private void SetupFSM()
        {
            ViewerState[] playerStates = movementSwitcher.MovementPresets.Select(preset => preset.viewerState).Distinct().ToArray();

            fsm = new FirstPersonViewerStateMachine(this, input, playerStates);
        }

        private void Update()
        {
            CheckGroundCollision();

            fsm.OnUpdate();

            transform.position += Vector3.up * velocity.y * Time.deltaTime;

            if (input.ResetInput.triggered) OnResetToGround?.Invoke();
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

        private void SetMovementModus(MovementPresets movementPresets)
        {
            if (movementPresets.viewMesh != null)
            {
                meshFilter.mesh = movementPresets.viewMesh;
                meshRenderer.materials = movementPresets.meshMaterials;
            }
            else meshFilter.mesh = null;

            FirstPersonCamera.SetCameraConstrain(movementPresets.viewerState.CameraConstrain);

            fsm.SwitchState(movementPresets.viewerState);
        }

        public void SetupState(Vector3 cameraPosition, Vector3 playerEuler, Vector3 cameraEuler, float cameraHeightOffset)
        {
            transform.position = cameraPosition + Vector3.down * cameraHeightOffset;
            FirstPersonCamera.transform.localPosition = Vector3.up * cameraHeightOffset;

            transform.rotation = Quaternion.Euler(playerEuler);
            FirstPersonCamera.transform.localRotation = Quaternion.Euler(cameraEuler);
        }

        private void ResetToStart()
        {
            transform.position = startPosition.ToUnity();
            transform.rotation = startRotation;
            yPositionTarget = transform.position.y;

            transform.position += Vector3.up * fsm.CurrentState.GetGroundHeightOffset();
        }

        //Only way to block input and not include checks in every state.
        public Vector2 GetMoveInput()
        {
            if (input.LockInput) return Vector2.zero;
            else return input.MoveAction.ReadValue<Vector2>();
        }

        private void ResetToGround()
        {
            raycaster.GetWorldPointFromDirectionAsync(transform.position + Vector3.up * 100, Vector3.down, (point, hit) =>
            {
                if (hit)
                {
                    SetVelocity(Vector2.zero);
                    yPositionTarget = point.y;
                    transform.position = new Vector3(transform.position.x, yPositionTarget + fsm.CurrentState.GetGroundHeightOffset(), transform.position.z);
                }
            }, snappingCullingMask);
        }

        public void SetVelocity(Vector2 velocity) => this.velocity = velocity;
    }
}