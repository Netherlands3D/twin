using DG.Tweening;
using Netherlands3D.Events;
using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class ViewerVehicularState : ViewerState
    {
        private float currentSpeed;

        private float acceleration;
        private float deceleration;
        private float turnSpeed;

        public override void OnEnter()
        {
            viewer.transform.position = viewer.transform.position + Vector3.down * viewer.MovementModus.viewHeight;
            viewer.FirstPersonCamera.transform.localPosition = Vector3.up * viewer.MovementModus.viewHeight;

            //Get Rotation this depends on the current Camera Constrain
            Vector3 euler = viewer.FirstPersonCamera.GetEulerRotation();
            viewer.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            viewer.FirstPersonCamera.transform.localRotation = Quaternion.Euler(euler.x, 0f, 0f);

            currentSpeed = 0;

            viewer.GetGroundPosition();
            ViewerEvents.OnChangeCameraConstrain?.Invoke(CameraConstrain.CONTROL_NONE);
            ViewerEvents.OnResetToGround += ResetToGround;

            ViewerSettingsEvents<float>.AddListener("Acceleration", SetAcceleration);
            ViewerSettingsEvents<float>.AddListener("Deceleration", SetDeceleration);
            ViewerSettingsEvents<float>.AddListener("TurnSpeed", SetTurnSpeed);
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = viewer.GetMoveInput();
            MoveVehicle(moveInput);

            viewer.SnapToGround();

            viewer.ApplyGravity();
        }

        public override void OnExit()
        {
            ViewerEvents.OnResetToGround -= ResetToGround;

            ViewerSettingsEvents<float>.RemoveListener("Acceleration", SetAcceleration);
            ViewerSettingsEvents<float>.RemoveListener("Deceleration", SetDeceleration);
            ViewerSettingsEvents<float>.RemoveListener("TurnSpeed", SetTurnSpeed);
        }

        private void MoveVehicle(Vector2 moveInput)
        {
            float speedMultiplier = input.SprintAction.IsPressed() ? viewer.MovementModus.speedMultiplier : 1;

            float targetSpeed = moveInput.y * viewer.MovementSpeed * speedMultiplier;

            if (Mathf.Abs(targetSpeed) > 0.1f && viewer.isGrounded) currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * speedMultiplier * Time.deltaTime);
            else currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * speedMultiplier * Time.deltaTime);

            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                float turn = moveInput.x * turnSpeed * Time.deltaTime * Mathf.Sign(currentSpeed);
                transform.Rotate(Vector3.up * turn);
            }
            else
            {
                float turn = moveInput.x * turnSpeed * .2f * Time.deltaTime;
                transform.Rotate(Vector3.up * turn);
            }

            if (currentSpeed > 0)
            {
                viewer.GetGroundPosition();
                ViewerEvents.OnCameraRotation?.Invoke(viewer.FirstPersonCamera.transform.forward);
            }
        }

        private void ResetToGround()
        {
            currentSpeed = 0;
        }

        private void SetAcceleration(float acceleration) => this.acceleration = acceleration;
        private void SetDeceleration(float deceleration) => this.deceleration = deceleration;
        private void SetTurnSpeed(float turnSpeed) => this.turnSpeed = turnSpeed;
    }
}
