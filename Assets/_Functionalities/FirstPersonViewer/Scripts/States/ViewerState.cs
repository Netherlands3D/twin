using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class ViewerState : MonoBehaviour
    {
        protected FirstPersonViewerStateMachine owner;
        //protected PlayerData playerData;
        //protected PlayerInputHandler playerInput;
        protected FirstPersonViewer viewer;

        protected Transform transform;

        public void Initialize(FirstPersonViewerStateMachine owner, FirstPersonViewer viewer)
        {
            this.owner = owner;
            //playerData = player.PlayerData;
            //playerInput = player.PlayerInputHandler;
            this.viewer = viewer;

            transform = viewer.transform;
        }


        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
    }
}
