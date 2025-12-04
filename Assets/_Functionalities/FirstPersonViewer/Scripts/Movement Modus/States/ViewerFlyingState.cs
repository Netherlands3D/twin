using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Flying State", menuName = "ScriptableObjects/FirstPersonViewer/States/Flying State")]
    public class ViewerFlyingState : ViewerState
    {
        [SerializeField] private MovementBoolSetting smoothFlight;
        private float currentSpeed;
        private float verticalVelocity;
        private float verticalSmoothTime = 0.4f;
        private Vector3 smoothVelocity = Vector3.zero;

        public override void OnEnter()
        {
            base.OnEnter();

            Vector3 camPosition = viewer.FirstPersonCamera.GetPreviousCameraHeight();
            //Get Rotation this depends on the current Camera Constrain
            Vector3 eulerRotation = viewer.FirstPersonCamera.GetStateRotation();
            viewer.SetupState(camPosition, eulerRotation, Vector3.zero, 0);

            currentSpeed = 0;
            viewer.SetVelocity(Vector2.zero);

            viewer.OnResetToGround += ResetCurrentSpeed;
            viewer.OnResetToStart += ResetCurrentSpeed;
        }

        public override void OnExit()
        {
            viewer.OnResetToGround -= ResetCurrentSpeed;
            viewer.OnResetToStart -= ResetCurrentSpeed;

            viewer.FirstPersonCamera.SetCameraRotationDampening(false);
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = GetMoveInput();
            viewer.FirstPersonCamera.SetCameraRotationDampening(smoothFlight.Value);

            if (smoothFlight.Value) MoveFreeCamSmooth(moveInput);
            else MoveFreeCam(moveInput);

            float verticalInput = input.VerticalMoveAction.ReadValue<float>();
            if (smoothFlight.Value) MoveVerticalSmooth(verticalInput);
            else MoveVertical(verticalInput);

        }

        private void MoveFreeCam(Vector2 moveInput)
        {
            if (moveInput.magnitude <= 0) return;

            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;

            float calculatedSpeed = MovementSpeed * (input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1);

            transform.Translate(direction * calculatedSpeed * Time.deltaTime, Space.World);
        }

        private void MoveFreeCamSmooth(Vector2 moveInput)
        {
            float currentMultiplier = input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1;

            Vector3 direction = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            Vector3 targetSpeed = moveInput * MovementSpeed;

            if (targetSpeed.magnitude > 0.1f) currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed.magnitude, (MovementSpeed * .15f) * currentMultiplier * Time.deltaTime);
            else currentSpeed = Mathf.MoveTowards(currentSpeed, 0, (MovementSpeed * .10f) * currentMultiplier * Time.deltaTime);


            Vector3 targetTransformation = direction * currentSpeed;
            if (Vector3.Dot(smoothVelocity, targetTransformation) < 0f) smoothVelocity = Vector3.zero;
            smoothVelocity = Vector3.Slerp(smoothVelocity, targetTransformation, 10f * Time.deltaTime);

            transform.Translate(smoothVelocity * Time.deltaTime, Space.World);
        }



        private void MoveVertical(float verticalInput)
        {
            if (verticalInput == 0) return;

            float calculatedSpeed = MovementSpeed * (input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1);

            transform.Translate(Vector3.up * verticalInput * calculatedSpeed * Time.deltaTime, Space.World);
        }
        
        private void MoveVerticalSmooth(float verticalInput)
        {
            float targetSpeed = 0f;

            if (verticalInput != 0f)
            {
                float calculatedSpeed = MovementSpeed * .4f * (input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1);

                targetSpeed = verticalInput * calculatedSpeed;
            }

            float smoothedSpeed = Mathf.SmoothDamp(
                verticalVelocity,
                targetSpeed,
                ref verticalVelocity,
                verticalSmoothTime
            );

            transform.Translate(Vector3.up * smoothedSpeed * Time.deltaTime, Space.World);
        }

        private void ResetCurrentSpeed()
        {
            currentSpeed = 0;
        }
    }
}
