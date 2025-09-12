using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class ViewerWalkingState : ViewerState
    {
        public override void OnEnter()
        {
            viewer.transform.position = viewer.transform.position + Vector3.down * viewer.MovementModus.viewHeight;
            viewer.FirstPersonCamera.transform.localPosition = Vector3.up * viewer.MovementModus.viewHeight;

            float pitch = viewer.FirstPersonCamera.transform.localEulerAngles.x;
            Vector3 euler = viewer.transform.eulerAngles;

            viewer.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            viewer.FirstPersonCamera.transform.localRotation = Quaternion.Euler(euler.x, 0f, 0f);

            viewer.GetGroundPosition();
            viewer.FirstPersonCamera.UpdateCameraConstrain(CameraConstrain.CONTROL_Y);
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = viewer.MoveAction.ReadValue<Vector2>();
            if (moveInput.magnitude > 0)
            {
                MovePlayer(moveInput);
            }

            viewer.SnapToGround();

            Jump();

            viewer.ApplyGravity();
        }

        private void MovePlayer(Vector2 moveInput)
        {
            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float calculatedSpeed = viewer.MovementSpeed * (viewer.SprintAction.IsPressed() ? viewer.MovementModus.runningMultiplier : 1);

            transform.Translate(direction * calculatedSpeed * Time.deltaTime, Space.World);
            viewer.GetGroundPosition();
        }

        private void Jump()
        {
            if (viewer.JumpAction.triggered && viewer.isGrounded)
            {
                viewer.SetVelocity(new Vector2(viewer.Velocity.x, viewer.MovementModus.jumpHeight));
                viewer.isGrounded = false;
            }
        }
    }
}
