using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Walking State", menuName = "ScriptableObjects/FirstPersonViewer/States/Walking State")]
    public class ViewerWalkingState : ViewerState
    {
        private float jumpForce;

        [SerializeField] private MovementFloatSetting jumpFoceSetting;

        public override void OnEnter()
        {
            base.OnEnter();

            viewer.transform.position += Vector3.down * viewer.FirstPersonCamera.CameraHeightOffset;
            viewer.FirstPersonCamera.transform.localPosition = Vector3.up * viewer.FirstPersonCamera.CameraHeightOffset;

            //Get Rotation this depends on the current Camera Constrain
            Vector3 euler = viewer.FirstPersonCamera.GetEulerRotation();
            viewer.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            viewer.FirstPersonCamera.transform.localRotation = Quaternion.Euler(euler.x, 0f, 0f);

            viewer.GetGroundPosition();

            jumpFoceSetting.OnValueChanged.AddListener(SetJumpForce);
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = viewer.GetMoveInput();
            if (moveInput.magnitude > 0)
            {
                MovePlayer(moveInput);
            }

            viewer.SnapToGround();

            if(!input.LockInput) Jump();

            viewer.ApplyGravity();
        }

        public override void OnExit()
        {
            base.OnExit();
            jumpFoceSetting.OnValueChanged.RemoveListener(SetJumpForce);
        }

        private void MovePlayer(Vector2 moveInput)
        {
            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float calculatedSpeed = viewer.MovementSpeed * (input.SprintAction.IsPressed() ? SpeedMultiplier : 1);

            transform.Translate(direction * calculatedSpeed * Time.deltaTime, Space.World);
            viewer.GetGroundPosition();
        }

        private void Jump()
        {
            if (input.JumpAction.triggered && viewer.isGrounded)
            {
                viewer.SetVelocity(new Vector2(viewer.Velocity.x, jumpForce));
                viewer.isGrounded = false;
            }
        }

        private void SetJumpForce(float force) => jumpForce = force;
    }
}
