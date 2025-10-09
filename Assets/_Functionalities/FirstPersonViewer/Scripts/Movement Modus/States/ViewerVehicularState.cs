using DG.Tweening;
using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class ViewerVehicularState : ViewerState
    {
        private float currentSpeed;

        public override void OnEnter()
        {
            if (viewer.FirstPersonCamera.transform.localPosition.y == 0)
            {
                viewer.transform.position = viewer.transform.position + Vector3.down * viewer.MovementModus.viewHeight;
                viewer.FirstPersonCamera.transform.localPosition = Vector3.up * viewer.MovementModus.viewHeight;
            }

            //Get Rotation this depends on the current Camera Constrain
            Vector3 euler = viewer.FirstPersonCamera.GetEulerRotation();
            viewer.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            viewer.FirstPersonCamera.transform.localRotation = Quaternion.Euler(euler.x, 0f, 0f);

            currentSpeed = 0;

            viewer.GetGroundPosition();
            ViewerEvents.OnChangeCameraConstrain?.Invoke(CameraConstrain.CONTROL_NONE);
            ViewerEvents.OnResetToGround += ResetToGround;
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
        }

        private void MoveVehicle(Vector2 moveInput)
        {
            float speedMultiplier = input.SprintAction.IsPressed() ? viewer.MovementModus.speedMultiplier : 1;

            float targetSpeed = moveInput.y * viewer.MovementSpeed * speedMultiplier;

            if (Mathf.Abs(targetSpeed) > 0.1f && viewer.isGrounded) currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, viewer.MovementModus.acceleration * speedMultiplier * Time.deltaTime);
            else currentSpeed = Mathf.MoveTowards(currentSpeed, 0, viewer.MovementModus.deceleration * speedMultiplier * Time.deltaTime);

            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                float turn = moveInput.x * viewer.MovementModus.turnSpeed * Time.deltaTime * Mathf.Sign(currentSpeed);
                transform.Rotate(Vector3.up * turn);
            } else
            {
                float turn = moveInput.x * viewer.MovementModus.turnSpeed * .2f * Time.deltaTime;
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
    }
}
