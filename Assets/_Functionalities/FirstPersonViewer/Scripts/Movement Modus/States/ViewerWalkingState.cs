using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Walking State", menuName = "ScriptableObjects/FirstPersonViewer/States/Walking State")]
    public class ViewerWalkingState : ViewerState
    {
        [SerializeField] private MovementFloatSetting jumpFoceSetting;

        public override void OnEnter()
        {
            base.OnEnter();

            //Get Rotation this depends on the current Camera Constrain
            Vector3 euler = viewer.FirstPersonCamera.GetStateRotation();

            viewer.SetupState(transform.position, new Vector3(0f, euler.y, 0f), new Vector3(euler.x, 0f, 0f), viewer.FirstPersonCamera.CameraHeightOffset);

            viewer.GetGroundPosition();
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = GetMoveInput();
            if (moveInput.magnitude > 0)
            {
                MovePlayer(moveInput);
            }

            viewer.SnapToGround();

            if(!input.LockInput) Jump();

            viewer.ApplyGravity();
        }

        private void MovePlayer(Vector2 moveInput)
        {
            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float calculatedSpeed = MovementSpeed * (input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1);

            transform.Translate(direction * calculatedSpeed * Time.deltaTime, Space.World);
            viewer.GetGroundPosition();
        }

        private void Jump()
        {
            if (input.SpaceAction.triggered && viewer.isGrounded)
            {
                viewer.SetVelocity(new Vector2(viewer.Velocity.x, jumpFoceSetting.Value));
                viewer.isGrounded = false;
            }
        }
    }
}
