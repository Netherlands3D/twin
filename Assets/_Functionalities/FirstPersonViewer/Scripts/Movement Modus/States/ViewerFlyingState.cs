using NUnit.Framework.Constraints;
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
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = GetMoveInput();
            viewer.FirstPersonCamera.SetCameraRotationDampening(smoothFlight.Value);

            float verticalInput = input.VerticalMoveAction.ReadValue<float>();

            if (smoothFlight.Value)
            {
                MoveFreeCamSmooth(moveInput);
                MoveVerticalSmooth(verticalInput);
            }
            else
            {
                MoveFreeCam(moveInput);
                MoveVertical(verticalInput);
            }
        }

        private void MoveFreeCam(Vector2 moveInput)
        {
            if (moveInput.magnitude <= 0) return;

            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float calculatedSpeed = MovementSpeed * (input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1);

            transform.Translate(direction * calculatedSpeed * Time.deltaTime, Space.World);
        }

        private void MoveFreeCamSmooth(Vector2 moveInput)
        {
            float currentMultiplier = input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1;
            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            Vector3 targetSpeed = moveInput * MovementSpeed * currentMultiplier;

            //Accelleratie/ decelleratie
            if (targetSpeed.magnitude > 0.1f) currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed.magnitude, (MovementSpeed * .15f) * currentMultiplier * Time.deltaTime);
            else currentSpeed = Mathf.MoveTowards(currentSpeed, 0, (MovementSpeed * .5f) * currentMultiplier * Time.deltaTime);

            Vector3 targetTransformation = direction * currentSpeed;

            //Turn more than 90 degrees - reset momentum
            if (Vector3.Dot(smoothVelocity, targetTransformation) < 0f || Vector3.Dot(moveInput, lastMovementInput) < 0f)
                smoothVelocity = Vector3.zero;

            //No input direction, take the current momentum from smoothVelocity and apply it as it decellerates.
            if (direction == Vector3.zero)
                targetTransformation = smoothVelocity.normalized * currentSpeed;

            smoothVelocity = Vector3.Slerp(smoothVelocity, targetTransformation, 10f * Time.deltaTime);

            transform.Translate(smoothVelocity * Time.deltaTime, Space.World);

            //Save last Input for snap-turning.
            lastMovementInput = moveInput;
        }

        private void MoveVertical(float verticalInput)
        {
            if (verticalInput == 0) return;

            float calculatedSpeed = MovementSpeed * (input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1);

            transform.Translate(Vector3.up * verticalInput * calculatedSpeed * Time.deltaTime, Space.World);
        }

        private void MoveVerticalSmooth(float verticalInput)
        {
            float targetSpeed = 0f;

            if (verticalInput != 0f)
            {
                float calculatedSpeed = MovementSpeed * .4f * (input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1);

                targetSpeed = verticalInput * calculatedSpeed;
            }

            float smoothedSpeed = Mathf.SmoothDamp(
                verticalVelocity,
                targetSpeed,
                ref verticalVelocity,
                verticalSmoothTime
            );

            transform.Translate(Vector3.up * smoothedSpeed * Time.deltaTime, Space.World);
        }

        private void ResetCurrentSpeed()
        {
            currentSpeed = 0;
        }

        private Vector3 ClampVector3(Vector3 vector, float min, float max)
        {
            return new Vector3(Mathf.Clamp(vector.x, min, max), Mathf.Clamp(vector.y, min, max), Mathf.Clamp(vector.z, min, max));
        }
    }
}
