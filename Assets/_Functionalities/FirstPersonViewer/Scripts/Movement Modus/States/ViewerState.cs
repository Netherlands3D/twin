using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public abstract class ViewerState : ScriptableObject
    {
        protected FirstPersonViewerInput input;
        protected FirstPersonViewer viewer;
        protected Transform transform;

        [field: SerializeField] public CameraConstrain CameraConstrain { private set; get; }
        protected float MovementSpeed { private set; get; }

        [Header("Viewer Settings")]
        [SerializeField] private MovementFloatSetting maxSpeedSetting;
        [SerializeField] protected MovementFloatSetting speedMultiplierSetting;
        [SerializeField] protected MovementFloatSetting groundResetHeightOffsetSetting;

        public void Initialize(FirstPersonViewer viewer, FirstPersonViewerInput input)
        {
            this.input = input;
            this.viewer = viewer;

            transform = viewer.transform;
        }

        public virtual void OnEnter()
        {
            maxSpeedSetting.OnValueChanged.AddListener(SetMaxSpeed);
        }

        public virtual void OnExit()
        {
            maxSpeedSetting.OnValueChanged.RemoveListener(SetMaxSpeed);
        }

        public virtual void OnUpdate() { }

        public float GetGroundHeightOffset() => groundResetHeightOffsetSetting.Value;
        
        //We need to calculate the speed from Km/h to m/s
        private void SetMaxSpeed(float speed) => MovementSpeed = speed / 3.6f;
    }
}
