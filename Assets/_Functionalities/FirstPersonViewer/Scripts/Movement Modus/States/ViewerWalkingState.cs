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

        public override void OnEnter()
        {
            //Prevents the up teleportation when switching to flying state and back.
            if (viewer.FirstPersonCamera.transform.localPosition.y == 0)
            {
                viewer.transform.position = viewer.transform.position + Vector3.down * viewer.MovementModus.viewHeight;
                viewer.FirstPersonCamera.transform.localPosition = Vector3.up * viewer.MovementModus.viewHeight;
            }

            //Get Rotation this depends on the current Camera Constrain
            Vector3 euler = viewer.FirstPersonCamera.GetEulerRotation();
            viewer.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            viewer.FirstPersonCamera.transform.localRotation = Quaternion.Euler(euler.x, 0f, 0f);

            viewer.GetGroundPosition();
            ViewerEvents.OnChangeCameraConstrain?.Invoke(CameraConstrain.CONTROL_Y);

            //if(viewerData.ViewerSetting.ContainsKey("JumpForce")) jumpForce = (float)viewerData.ViewerSetting["JumpForce"];

            ViewerSettingsEvents<float>.AddListener("JumpForce", SetJumpForce);
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
            ViewerSettingsEvents<float>.RemoveListener("JumpForce", SetJumpForce);
        }

        private void MovePlayer(Vector2 moveInput)
        {
            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float calculatedSpeed = viewer.MovementSpeed * (input.SprintAction.IsPressed() ? viewer.MovementModus.speedMultiplier : 1);

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
