using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class ViewerState : MonoBehaviour
    {
        protected FirstPersonViewerStateMachine owner;
        protected FirstPersonViewerInput input;
        protected FirstPersonViewer viewer;

        protected Transform transform;

        public void Initialize(FirstPersonViewerStateMachine owner, FirstPersonViewer viewer, FirstPersonViewerInput input)
        {
            this.owner = owner;
            this.input = input;
            this.viewer = viewer;

            transform = viewer.transform;
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
    }
}
