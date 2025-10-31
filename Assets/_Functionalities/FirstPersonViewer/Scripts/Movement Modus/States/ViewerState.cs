using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public abstract class ViewerState : ScriptableObject
    {
        protected FirstPersonViewerInput input;
        protected FirstPersonViewer viewer;
        protected Transform transform;

        [field:SerializeField] public CameraConstrain CameraConstrain { private set; get; }

        protected float SpeedMultiplier { private set; get; }
        public float GroundResetHeightOffset { private set; get; }

        [Header("Viewer Settings")]
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
            speedMultiplierSetting.OnValueChanged.AddListener(SetMultiplierSpeed);
            groundResetHeightOffsetSetting.OnValueChanged.AddListener(SetResetHeightOffset);
        }

        public virtual void OnExit()
        {
            speedMultiplierSetting.OnValueChanged.RemoveListener(SetMultiplierSpeed);
            groundResetHeightOffsetSetting.OnValueChanged.RemoveListener(SetResetHeightOffset);
        }

        public virtual void OnUpdate() { }

        private void SetMultiplierSpeed(float speed) => SpeedMultiplier = speed;
        private void SetResetHeightOffset(float heightOffset) => GroundResetHeightOffset = heightOffset;
    }
}
