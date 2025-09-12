using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class ViewerVehicularState : ViewerState
    {
        //Should be moved to ScriptableObject
        [SerializeField] float acceleration = 10f;
        [SerializeField] float deceleration = 5f;
        [SerializeField] private float turnSpeed = 100f;
        private float tempSpeed = 20f;

        private float currentSpeed;

        public override void OnEnter()
        {
            viewer.transform.position = viewer.transform.position + Vector3.down * viewer.MovementModus.viewHeight;
            viewer.FirstPersonCamera.transform.localPosition = Vector3.up * viewer.MovementModus.viewHeight;

            float pitch = viewer.FirstPersonCamera.transform.localEulerAngles.x;
            Vector3 euler = viewer.transform.eulerAngles;

            viewer.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            viewer.FirstPersonCamera.transform.localRotation = Quaternion.Euler(euler.x, 0f, 0f);

            currentSpeed = 0;

            viewer.GetGroundPosition();
            viewer.FirstPersonCamera.UpdateCameraConstrain(CameraConstrain.CONTROL_NONE);
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = viewer.MoveAction.ReadValue<Vector2>();
            MoveVehicle(moveInput);

            viewer.SnapToGround();

            viewer.ApplyGravity();
        }

        private void MoveVehicle(Vector2 moveInput)
        {
            float targetSpeed = moveInput.y * tempSpeed;//viewer.MovementSpeed;

            if (Mathf.Abs(targetSpeed) > 0.1f && viewer.isGrounded) currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
            else currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);

            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                float turn = moveInput.x * turnSpeed * Time.deltaTime * Mathf.Sign(currentSpeed);
                transform.Rotate(Vector3.up * turn);
            }

            if(currentSpeed > 0) viewer.GetGroundPosition();
        }
    }
}
