using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Vehicular State", menuName = "ScriptableObjects/FirstPersonViewer/States/Vehicular State")]
    public class ViewerVehicularState : ViewerState
    {
        private float currentSpeed;

        [SerializeField] private MovementFloatSetting accelerationSetting;
        [SerializeField] private MovementFloatSetting decelerationSetting;
        [SerializeField] private MovementFloatSetting turnSpeedSetting;

        [Header("Viewer Labels")]
        [SerializeField] private MovementLabelSetting currentSpeedLabel;

        public override void OnEnter()
        {
            base.OnEnter();

            //Get Rotation this depends on the current Camera Constrain
            Vector3 euler = viewer.FirstPersonCamera.GetEulerRotation();
            viewer.SetupState(transform.position, new Vector3(0f, euler.y, 0f), new Vector3(euler.x, 0f, 0f), viewer.FirstPersonCamera.CameraHeightOffset);

            viewer.GetGroundPosition();

            currentSpeed = 0;

            viewer.OnResetToGround += ResetToGround;
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = GetMoveInput();
            MoveVehicle(moveInput);

            viewer.SnapToGround();

            viewer.ApplyGravity();
        }

        public override void OnExit()
        {
            viewer.OnResetToGround -= ResetToGround;
        }

        private void MoveVehicle(Vector2 moveInput)
        {
            float currentMultiplier = input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1;
            bool isGoingBackwards = moveInput.y < 0;
            currentMultiplier *= (isGoingBackwards ? .3f : 1);

            float targetSpeed = moveInput.y * MovementSpeed * currentMultiplier;

            if (Mathf.Abs(targetSpeed) > 0.1f && viewer.isGrounded) currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationSetting.Value * currentMultiplier * Time.deltaTime);
            else currentSpeed = Mathf.MoveTowards(currentSpeed, 0, decelerationSetting.Value * currentMultiplier * Time.deltaTime);

            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                float turn = moveInput.x * turnSpeedSetting.Value * Time.deltaTime * Mathf.Sign(currentSpeed);
                transform.Rotate(Vector3.up * turn);
            }
            else
            {
                float turn = moveInput.x * turnSpeedSetting.Value * .2f * Time.deltaTime;
                transform.Rotate(Vector3.up * turn);
            }

            if (Mathf.Abs(currentSpeed) > 0)
            {
                viewer.GetGroundPosition();

                int speedInKilometers = Mathf.RoundToInt(currentSpeed * 3.6f);

                currentSpeedLabel.Value = speedInKilometers.ToString();
            }
        }

        private void ResetToGround()
        {
            currentSpeed = 0;
        }
    }
}
