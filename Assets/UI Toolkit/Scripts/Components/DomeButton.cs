using Netherlands3D.Masking;
using Netherlands3D.Twin;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DomeButton : VisualElement
    {
        public bool Dragging => dragging;
        private bool dragging = false;
        
        private Vector3 startScale = Vector3.one;
        private Vector3 pointerStartDragPosition;
        private Vector3 pointerObjectStartPosition;
        private float startDistance;
        
        public DomeButton()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            Button button = this.Q<Button>();
            var dragManipulator = new DragManipulator(0, TrickleDown.TrickleDown);
            button.AddManipulator(dragManipulator);
            dragManipulator.DragStarted.AddListener(OnDragStarted);
            dragManipulator.DragEnded.AddListener(OnDragEnded);
            
            button.RegisterCallback<PointerEnterEvent>(OnFPVPointerEnter);

            button.RegisterCallback<PointerLeaveEvent>(OnFPVPointerLeave);
        }

        private void OnFPVPointerEnter(PointerEnterEvent evt)
        {
            if(!dragging)
                PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRAB);
        }
        
        private void OnFPVPointerLeave(PointerLeaveEvent evt)
        {
            if(!dragging)
                PointerStyle.CancelCursorChange(this);
        }
        
        private void OnDragStarted(Vector2 startPosition)
        {
            PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRABBING);
            
            VisualDome dome = App.Dome.Spawner.DomeVisualisation;
            pointerStartDragPosition = App.Cameras.ActiveCamera.ScreenToViewportPoint(Pointer.current.position.ReadValue());
            pointerObjectStartPosition = App.Cameras.ActiveCamera.WorldToViewportPoint(dome.transform.position);
            pointerObjectStartPosition.z = 0; //Remove depth

            startDistance = Vector3.Distance(pointerStartDragPosition, pointerObjectStartPosition);

            startScale = dome.transform.localScale;
            AddToClassList("grabbing");
            dragging = true;
        } 
        
        private void OnDragEnded(Vector2 endPosition)
        {
            Debug.Log("drag ended");
            dragging = false;
            RemoveFromClassList("grabbing");
            
            if (worldBound.Contains(endPosition))
                PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRAB); //pointer is still in the panel
            else
                PointerStyle.CancelCursorChange(this);
        }
        
        public Vector3 GetDistanceScale()
        {
            var pointerViewportPoint = App.Cameras.ActiveCamera.ScreenToViewportPoint(Pointer.current.position.ReadValue());
            float dist = Vector3.Distance(pointerViewportPoint, pointerObjectStartPosition);
            var distancePointerMoved = dist / startDistance;
            return startScale * distancePointerMoved;
        }
    }
}
