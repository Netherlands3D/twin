using Netherlands3D.Services;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class ViewerState : MonoBehaviour
    {
        protected FirstPersonViewerStateMachine owner;
        protected FirstPersonViewerInput input;
        protected FirstPersonViewer viewer;

        protected Transform transform;
        protected FirstPersonViewerData viewerData;

        public void Initialize(FirstPersonViewerStateMachine owner, FirstPersonViewer viewer, FirstPersonViewerInput input)
        {
            this.owner = owner;
            this.input = input;
            this.viewer = viewer;

            transform = viewer.transform;

            viewerData = FirstPersonViewerData.Instance; //ServiceLocator.GetService<FirstPersonViewerData>();

        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
    }
}
