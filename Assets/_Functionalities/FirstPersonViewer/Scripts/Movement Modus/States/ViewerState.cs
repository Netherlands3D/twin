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

        [Header("Visuals")]
        public string viewName;
        public Sprite viewIcon;
        public Mesh viewMesh;
        public Material[] meshMaterials;

        [Header("Editable Settings")]
        public SerializableViewerSettingsList editableSettings = new SerializableViewerSettingsList();

        [Header("Viewer Settings")]
        [SerializeField] private MovementFloatSetting maxSpeedSetting;
        [SerializeField] protected MovementFloatSetting speedMultiplierSetting;
        [SerializeField] protected MovementFloatSetting groundResetHeightOffsetSetting;

        public void Initialize(FirstPersonViewer viewer, FirstPersonViewerInput input)
        {
            this.input = input;
            this.viewer = viewer;

            transform = viewer.transform;
            maxSpeedSetting.OnValueChanged.AddListener(SetMaxSpeed);
        }

        public void Uninitialize()
        {
            maxSpeedSetting.OnValueChanged.RemoveListener(SetMaxSpeed);    
        }

        public virtual void OnEnter() { }

        public virtual void OnExit() { }

        public virtual void OnUpdate() { }

        public float GetGroundHeightOffset() => groundResetHeightOffsetSetting.Value;
        
        //We need to calculate the speed from Km/h to m/s
        private void SetMaxSpeed(float speed) => MovementSpeed = speed / 3.6f;

        protected Vector2 GetMoveInput()
        {
            if (input.LockInput) return Vector2.zero;
            else return input.MoveAction.ReadValue<Vector2>();
        }
    }
}
