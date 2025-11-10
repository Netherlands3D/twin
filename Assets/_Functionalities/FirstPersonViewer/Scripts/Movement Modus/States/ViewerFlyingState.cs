using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Flying State", menuName = "ScriptableObjects/FirstPersonViewer/States/Flying State")]
    public class ViewerFlyingState : ViewerState
    {
        public override void OnEnter()
        {
            base.OnEnter();

            Vector3 camPosition = viewer.FirstPersonCamera.transform.position;
            //Get Rotation this depends on the current Camera Constrain
            Vector3 eulerRotation = viewer.FirstPersonCamera.GetEulerRotation();
            viewer.SetupState(camPosition, eulerRotation, Vector3.zero, 0);

            viewer.SetVelocity(Vector2.zero);
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = GetMoveInput();
            MoveFreeCam(moveInput);

            float verticalInput = input.VerticalMoveAction.ReadValue<float>();
            MoveVertical(verticalInput);
        }

        private void MoveFreeCam(Vector2 moveInput)
        {
            if (moveInput.magnitude <= 0) return;

            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float calculatedSpeed = MovementSpeed * (input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1);

            transform.Translate(direction * calculatedSpeed * Time.deltaTime, Space.World);
        }

        private void MoveVertical(float verticalInput)
        {
            if (verticalInput == 0) return;

            float calculatedSpeed = MovementSpeed * (input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1);

            transform.Translate(Vector3.up * verticalInput * calculatedSpeed * Time.deltaTime, Space.World);
        }
    }
}
