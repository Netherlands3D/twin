using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class ViewerWalkingState : ViewerState
    {
        public override void OnEnter()
        {
            
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = viewer.MoveAction.ReadValue<Vector2>();
            if (moveInput.magnitude > 0)
            {
                MovePlayer(moveInput);
            }

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
            if (Keyboard.current.spaceKey.wasPressedThisFrame && viewer.isGrounded)
            {
                viewer.SetVelocity(new Vector2(viewer.Velocity.x, viewer.MovementModus.jumpHeight));
                viewer.isGrounded = false;
            }
        }
    }
}
