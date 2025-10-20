using Netherlands3D.FirstPersonViewer.Events;
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

            //Get Rotation this depends on the current Camera Constrain
            Vector3 eulerRotation = viewer.FirstPersonCamera.GetEulerRotation();
            viewer.transform.eulerAngles = eulerRotation;
            viewer.FirstPersonCamera.transform.localEulerAngles = Vector3.zero;

            viewer.SetVelocity(Vector2.zero);
            ViewerEvents.OnChangeCameraConstrain?.Invoke(CameraConstrain.CONTROL_BOTH);
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = viewer.GetMoveInput();
            MoveFreeCam(moveInput);

            float verticalInput = input.VerticalMoveAction.ReadValue<float>();
            MoveVertical(verticalInput);
        }

        private void MoveFreeCam(Vector2 moveInput)
        {
            if (moveInput.magnitude <= 0) return;

            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float calculatedSpeed = viewer.MovementSpeed * (input.SprintAction.IsPressed() ? viewer.MovementModus.speedMultiplier : 1);

            transform.Translate(direction * calculatedSpeed * Time.deltaTime, Space.World);
        }

        private void MoveVertical(float verticalInput)
        {
            if (verticalInput == 0) return;

            float calculatedSpeed = viewer.MovementSpeed * (input.SprintAction.IsPressed() ? viewer.MovementModus.speedMultiplier : 1);

            transform.Translate(Vector3.up * verticalInput * calculatedSpeed * Time.deltaTime, Space.World);
        }
    }
}
