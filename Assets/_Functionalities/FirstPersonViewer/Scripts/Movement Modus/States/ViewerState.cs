using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public abstract class ViewerState : MonoBehaviour
    {
        protected FirstPersonViewerStateMachine owner;
        protected FirstPersonViewerInput input;
        protected FirstPersonViewer viewer;

        protected Transform transform;
        protected FirstPersonViewerData viewerData;

        //[Header("Settings")]

        public void Initialize(FirstPersonViewerStateMachine owner, FirstPersonViewer viewer, FirstPersonViewerInput input)
        {
            this.owner = owner;
            this.input = input;
            this.viewer = viewer;

            transform = viewer.transform;

            viewerData = ServiceLocator.GetService<FirstPersonViewerData>();
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }

        public virtual void ResetToGround()
        {

        }
    }
}
