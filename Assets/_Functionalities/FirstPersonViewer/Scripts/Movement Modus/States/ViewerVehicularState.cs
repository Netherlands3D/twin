using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    [CreateAssetMenu(fileName = "Vehicular State", menuName = "ScriptableObjects/FirstPersonViewer/States/Vehicular State")]
    public class ViewerVehicularState : ViewerState
    {
        private float currentSpeed;

        private float acceleration;
        private float deceleration;
        private float turnSpeed;

        [SerializeField] private MovementFloatSetting accelerationSetting;
        [SerializeField] private MovementFloatSetting decelerationSetting;
        [SerializeField] private MovementFloatSetting turnSpeedSetting;

        [Header("Viewer Labels")]
        [SerializeField] private MovementLabelSetting currentSpeedLabel;

        public override void OnEnter()
        {
            base.OnEnter();

            currentSpeed = 0;

            ViewerEvents.OnChangeCameraConstrain?.Invoke(CameraConstrain.CONTROL_NONE);
            ViewerEvents.OnResetToGround += ResetToGround;

            accelerationSetting.OnValueChanged.AddListener(SetAcceleration);
            decelerationSetting.OnValueChanged.AddListener(SetDeceleration);
            turnSpeedSetting.OnValueChanged.AddListener(SetTurnSpeed);
        }

        public override void OnUpdate()
        {
            Vector2 moveInput = viewer.GetMoveInput();
            MoveVehicle(moveInput);

            viewer.SnapToGround();

            viewer.ApplyGravity();
        }

        public override void OnExit()
        {
            ViewerEvents.OnResetToGround -= ResetToGround;

            accelerationSetting.OnValueChanged.RemoveListener(SetAcceleration);
            decelerationSetting.OnValueChanged.RemoveListener(SetDeceleration);
            turnSpeedSetting.OnValueChanged.RemoveListener(SetTurnSpeed);
        }

        private void MoveVehicle(Vector2 moveInput)
        {
            float currentMultiplier = input.SprintAction.IsPressed() ? SpeedMultiplier : 1;
            bool isGoingBackwards = moveInput.y < 0;
            currentMultiplier *= (isGoingBackwards ? .3f : 1);

            float targetSpeed = moveInput.y * viewer.MovementSpeed * currentMultiplier;

            if (Mathf.Abs(targetSpeed) > 0.1f && viewer.isGrounded) currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * currentMultiplier * Time.deltaTime);
            else currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * currentMultiplier * Time.deltaTime);

            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);

            if (Mathf.Abs(currentSpeed) > 0.1f)
            {
                float turn = moveInput.x * turnSpeed * Time.deltaTime * Mathf.Sign(currentSpeed);
                transform.Rotate(Vector3.up * turn);
            }
            else
            {
                float turn = moveInput.x * turnSpeed * .2f * Time.deltaTime;
                transform.Rotate(Vector3.up * turn);
            }

            if (Mathf.Abs(currentSpeed) > 0)
            {
                viewer.GetGroundPosition();
                ViewerEvents.OnCameraRotation?.Invoke(viewer.FirstPersonCamera.transform.forward);

                int speedInKilometers = Mathf.RoundToInt(currentSpeed * 3.6f);

                currentSpeedLabel.OnValueChanged.Invoke(speedInKilometers.ToString());
            }
        }

        private void ResetToGround()
        {
            currentSpeed = 0;
        }

        private void SetAcceleration(float acceleration) => this.acceleration = acceleration;
        private void SetDeceleration(float deceleration) => this.deceleration = deceleration;
        private void SetTurnSpeed(float turnSpeed) => this.turnSpeed = turnSpeed;
    }
}
