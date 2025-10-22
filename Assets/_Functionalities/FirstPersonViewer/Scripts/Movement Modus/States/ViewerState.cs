using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public abstract class ViewerState : ScriptableObject
    {
        protected FirstPersonViewerStateMachine owner;
        protected FirstPersonViewerInput input;
        protected FirstPersonViewer viewer;

        protected Transform transform;

        [field: Header("Settings")]
        [field: SerializeField] protected float SpeedMultiplier { private set; get; }
        [field: SerializeField] public float GroundResetHeightOffset { private set; get; }

        [Header("Viewer Settings")]
        [SerializeField] protected MovementLabel viewHeightSetting;

        public void Initialize(FirstPersonViewerStateMachine owner, FirstPersonViewer viewer, FirstPersonViewerInput input)
        {
            this.owner = owner;
            this.input = input;
            this.viewer = viewer;

            transform = viewer.transform;
        }

        public virtual void OnEnter()
        {
            viewer.transform.position += Vector3.down * viewer.FirstPersonCamera.CameraHeightOffset;
            viewer.FirstPersonCamera.transform.localPosition = Vector3.up * viewer.FirstPersonCamera.CameraHeightOffset;

            //Get Rotation this depends on the current Camera Constrain
            Vector3 euler = viewer.FirstPersonCamera.GetEulerRotation();
            viewer.transform.rotation = Quaternion.Euler(0f, euler.y, 0f);
            viewer.FirstPersonCamera.transform.localRotation = Quaternion.Euler(euler.x, 0f, 0f);

            viewer.GetGroundPosition();
        }

        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
    }
}
