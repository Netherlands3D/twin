using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class ViewerFlyingState : ViewerState
    {
        public override void OnEnter()
        {
            Vector3 camPosition = viewer.FirstPersonCamera.transform.position;
            viewer.transform.position = camPosition;
            viewer.FirstPersonCamera.transform.localPosition = Vector3.zero;

            Quaternion camRotation = viewer.FirstPersonCamera.transform.rotation;
            viewer.transform.rotation = camRotation;
            viewer.FirstPersonCamera.transform.localRotation = Quaternion.identity;

            viewer.SetVelocity(Vector2.zero);
            viewer.FirstPersonCamera.UpdateCameraConstrain(CameraConstrain.CONTROL_BOTH);
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = viewer.MoveAction.ReadValue<Vector2>();
            if (moveInput.magnitude > 0)
            {
                MoveFreeCam(moveInput);
            }

            float verticalInput = viewer.VerticalMoveAction.ReadValue<float>();
            if (verticalInput != 0)
            {
                MoveVertical(verticalInput);
            }
        }

        private void MoveFreeCam(Vector2 moveInput)
        {
            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float calculatedSpeed = viewer.MovementSpeed * (viewer.SprintAction.IsPressed() ? viewer.MovementModus.runningMultiplier : 1);

            transform.Translate(direction * calculatedSpeed * Time.deltaTime, Space.World);
        }

        private void MoveVertical(float verticalInput)
        {
            float calculatedSpeed = viewer.MovementSpeed * (viewer.SprintAction.IsPressed() ? viewer.MovementModus.runningMultiplier : 1);

            transform.Translate(Vector3.up * verticalInput * calculatedSpeed * Time.deltaTime, Space.World);
        }
    }
}
