using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Vehicular State", menuName = "ScriptableObjects/FirstPersonViewer/States/Vehicular State")]
    public class ViewerVehicularState : ViewerState
    {
        private float currentSpeed;
        private int lastSpeed;

        [SerializeField] private MovementFloatSetting accelerationSetting;
        [SerializeField] private MovementFloatSetting decelerationSetting;
        [SerializeField] private MovementFloatSetting turnSpeedSetting;

        [Header("Viewer Labels")]
        [SerializeField] private MovementLabelSetting currentSpeedLabel;

        public override void OnEnter()
        {
            base.OnEnter();

            //Get Rotation this depends on the current Camera Constrain
            Vector3 euler = viewer.FirstPersonCamera.GetStateRotation();
            viewer.SetupState(transform.position, new Vector3(0f, euler.y, 0f), new Vector3(euler.x, 0f, 0f), viewer.FirstPersonCamera.CameraHeightOffset);

            viewer.GetGroundPosition();

            currentSpeed = 0;

            viewer.OnResetToGround += ResetCurrentSpeed;
            viewer.OnResetToStart += ResetCurrentSpeed;
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = GetMoveInput();
            MoveVehicle(moveInput);
            if (input.SpaceAction.IsPressed())  Handbrake();

            viewer.SnapToGround();

            viewer.ApplyGravity();
        }

        public override void OnExit()
        {
            viewer.OnResetToGround -= ResetCurrentSpeed;
            viewer.OnResetToStart -= ResetCurrentSpeed;
        }

        private void MoveVehicle(Vector2 moveInput)
        {
            float currentMultiplier = input.SprintAction.IsPressed() ? speedMultiplierSetting.Value : 1;
            bool isGoingBackwards = moveInput.y < 0 && currentSpeed <= 0;
            currentMultiplier *= (isGoingBackwards ? .3f : 1);

            float targetSpeed = moveInput.y * MovementSpeed * currentMultiplier;

            if (Mathf.Abs(targetSpeed) > 0.1f && viewer.isGrounded) currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationSetting.Value * currentMultiplier * Time.deltaTime);
            else currentSpeed = Mathf.MoveTowards(currentSpeed, 0, decelerationSetting.Value * currentMultiplier * Time.deltaTime);

            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

            float turn = 0;
            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                turn = moveInput.x * turnSpeedSetting.Value * Time.deltaTime * Mathf.Sign(currentSpeed);
                transform.Rotate(Vector3.up * turn);
            }
            else
            {
                turn = moveInput.x * turnSpeedSetting.Value * .2f * Time.deltaTime;
                transform.Rotate(Vector3.up * turn);
            }

            movementVisualController.SetSteeringWheelRotation(turn * 30 * (isGoingBackwards ? -1 : 1));

            if (Mathf.Abs(currentSpeed) > 0) viewer.GetGroundPosition();

            int speedInKilometers = Mathf.RoundToInt(currentSpeed * 3.6f);
            if(speedInKilometers != lastSpeed)
            {
                currentSpeedLabel.Value = speedInKilometers.ToString();
                lastSpeed = speedInKilometers;
            }
        }

        private void Handbrake()
        {
            float handbrakeForce = 5;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, decelerationSetting.Value * handbrakeForce * Time.deltaTime);
        }

        private void ResetCurrentSpeed()
        {
            currentSpeed = 0;
        }
    }
}
