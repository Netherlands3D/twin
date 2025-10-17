using Netherlands3D.Events;
using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Walking State", menuName = "ScriptableObjects/FirstPersonViewer/States/Walking State")]
    public class ViewerWalkingState : ViewerState
    {
        private float jumpForce;

        [SerializeField] private MovementLabel jumpFoceSetting;

        public override void OnEnter()
        {
            base.OnEnter();

            ViewerEvents.OnChangeCameraConstrain?.Invoke(CameraConstrain.CONTROL_Y);

            //if(viewerData.ViewerSetting.ContainsKey("JumpForce")) jumpForce = (float)viewerData.ViewerSetting["JumpForce"];

            ViewerSettingsEvents<float>.AddListener(jumpFoceSetting, SetJumpForce);
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
            ViewerSettingsEvents<float>.RemoveListener(jumpFoceSetting, SetJumpForce);
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
