using System;
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
        public bool Hovering => hovering;
        
        private bool dragging = false;
        private bool hovering = false;
        
        private Vector3 startScale = Vector3.one;
        private Vector3 pointerStartDragPosition;
        private Vector3 pointerObjectStartPosition;
        private float startDistance;
        
        private PointerStyle.Style styleOnHover = PointerStyle.Style.AUTO;
        
        
        public DomeButton()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            Button button = this.Q<Button>();
            button.RegisterCallback<PointerDownEvent>(evt =>
            {
                VisualDome dome = App.Dome.Spawner.DomeVisualisation;
                pointerStartDragPosition = App.Cameras.ActiveCamera.ScreenToViewportPoint(Pointer.current.position.ReadValue());
                pointerObjectStartPosition = App.Cameras.ActiveCamera.WorldToViewportPoint(dome.transform.position);
                pointerObjectStartPosition.z = 0; //Remove depth

                startDistance = Vector3.Distance(pointerStartDragPosition, pointerObjectStartPosition);

                startScale = dome.transform.localScale;
                AddToClassList("grabbing");
                dragging = true;
            }, TrickleDown.TrickleDown);

            button.RegisterCallback<PointerUpEvent>(evt =>
            {
                dragging = false;
                RemoveFromClassList("grabbing");
            }, TrickleDown.TrickleDown);

            button.RegisterCallback<PointerEnterEvent>(evt =>
            {
                PointerStyle.ChangeCursor(styleOnHover);
                hovering = true;
            });

            button.RegisterCallback<PointerLeaveEvent>(evt =>
            {
                // Always change back cursor to CSS default 'auto'
                PointerStyle.ChangeCursor(PointerStyle.Style.AUTO);
                hovering = false;
            });
        }

        public Vector3 GetDistanceScale()
        {
            var pointerViewportPoint = App.Cameras.ActiveCamera.ScreenToViewportPoint(Pointer.current.position.ReadValue());
            float dist = Vector3.Distance(pointerViewportPoint, pointerObjectStartPosition);
            var distancePointerMoved = dist / startDistance;
            return startScale * distancePointerMoved;
        }

        public void SetStyleOnHover(PointerStyle.Style style)
        {
            this.styleOnHover = style;
        }
    }
}
