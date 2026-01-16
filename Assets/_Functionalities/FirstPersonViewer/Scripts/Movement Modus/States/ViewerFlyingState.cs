using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Flying State", menuName = "ScriptableObjects/FirstPersonViewer/States/Flying State")]
    public class ViewerFlyingState : ViewerState
    {
        [SerializeField] private MovementBoolSetting smoothFlight;
        [SerializeField] private MovementBoolSetting lockHeight;
        private float currentSpeed;
        private float verticalVelocity;
        private float verticalSmoothTime = 0.4f;
        private Vector3 smoothVelocity = Vector3.zero;
        private Vector3 lastMovementInput;

        private const float VERTICAL_DAMPENING_MULTIPLIER = .4f;
        private const float SMOOTH_VELOCITY_SLERP_FACTOR = 10f;
        private const float ACCELERATION_MULTIPLIER = .15f;
        private const float DECELERATOIN_MULTIPLIER = .5f;
        private const float MINIMAL_ACCELERATION_TARGET = .1f;

        public override void OnEnter()
        {
            base.OnEnter();

            Vector3 camPosition = viewer.FirstPersonCamera.GetPreviousCameraHeight();
            //Get Rotation this depends on the current Camera Constrain
            Vector3 eulerRotation = viewer.FirstPersonCamera.GetStateRotation();
            viewer.SetupState(camPosition, eulerRotation, Vector3.zero, 0);

            currentSpeed = 0;
            viewer.SetVelocity(Vector2.zero);

            viewer.OnResetToGround += ResetCurrentSpeed;
            viewer.OnResetToStart += ResetCurrentSpeed;
        }

        public override void OnExit()
        {
            viewer.OnResetToGround -= ResetCurrentSpeed;
            viewer.OnResetToStart -= ResetCurrentSpeed;

            viewer.FirstPersonCamera.SetCameraRotationDampening(false);
            ResetSmoothVelocity();
        }

        public override void OnUpdate()
        {
            if (DisableMovement()) return;

            Vector2 moveInput = GetMoveInput();
            viewer.FirstPersonCamera.SetCameraRotationDampening(smoothFlight.Value);

            float verticalInput = input.VerticalMoveAction.ReadValue<float>();

            if (smoothFlight.Value)//Smoothflight == on
            {
                MoveFreeCamSmooth(moveInput);
                MoveVerticalSmooth(verticalInput);
            }
            else
            {
                MoveFreeCam(moveInput);
                MoveVertical(verticalInput);
                ResetSmoothVelocity();
            }
        }

        private void MoveFreeCam(Vector2 moveInput)
        {
            if (moveInput.magnitude <= 0) return;

            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            transform.Translate(direction * CalculateSpeed() * Time.deltaTime, Space.World);
        }

        private void MoveFreeCamSmooth(Vector2 moveInput)
        {
            float currentMultiplier = input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1;
            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            Vector3 targetSpeed = moveInput * MovementSpeed * currentMultiplier;

            //Accelleratie/ decelleratie
            if (targetSpeed.magnitude > MINIMAL_ACCELERATION_TARGET) currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed.magnitude, (MovementSpeed * ACCELERATION_MULTIPLIER) * currentMultiplier * Time.deltaTime);
            else currentSpeed = Mathf.MoveTowards(currentSpeed, 0, (MovementSpeed * DECELERATOIN_MULTIPLIER) * currentMultiplier * Time.deltaTime);

            Vector3 targetTransformation = direction * currentSpeed;

            //Turn more than 90 degrees - reset momentum
            if (Vector3.Dot(smoothVelocity.normalized, targetTransformation.normalized) < 0f || Vector3.Dot(moveInput.normalized, lastMovementInput.normalized) < 0f)
            { smoothVelocity = Vector3.zero; currentSpeed = 0; }

            //No input direction, take the current momentum from smoothVelocity and apply it as it decellerates.
            if (direction == Vector3.zero)
                targetTransformation = smoothVelocity.normalized * currentSpeed;

            smoothVelocity = Vector3.Slerp(smoothVelocity, targetTransformation, SMOOTH_VELOCITY_SLERP_FACTOR * Time.deltaTime);

            transform.Translate(smoothVelocity * Time.deltaTime, Space.World);

            //Save last Input for snap-turning.
            lastMovementInput = moveInput;
        }

        private void MoveVertical(float verticalInput)
        {
            if (verticalInput == 0) return;

            transform.Translate(Vector3.up * verticalInput * CalculateSpeed() * Time.deltaTime, Space.World);
        }

        private void MoveVerticalSmooth(float verticalInput)
        {
            float targetSpeed = 0f;

            if (verticalInput != 0f)
            {
                targetSpeed = verticalInput * CalculateSpeed(VERTICAL_DAMPENING_MULTIPLIER);
            }

            float smoothedSpeed = Mathf.SmoothDamp(verticalVelocity, targetSpeed, ref verticalVelocity, verticalSmoothTime);

            transform.Translate(Vector3.up * smoothedSpeed * Time.deltaTime, Space.World);
        }

        private void ResetCurrentSpeed()
        {
            currentSpeed = 0;
        }

        private void ResetSmoothVelocity()
        {
            smoothVelocity = Vector3.zero;
        }

        private float CalculateSpeed(float dampeningMultiplier = 1)
        {
            return MovementSpeed * dampeningMultiplier * (input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1);
        }
    }
}
