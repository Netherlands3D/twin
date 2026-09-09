using System;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Compass = Netherlands3D.Twin.Cameras.Compass;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarNavigation : VisualElement
    {
        private Button North => this.Q<Button>("North");
        private Toggle Perspective => this.Q<Toggle>("Perspective");
        private Button FPV => this.Q<Button>("FPV");
        private bool isDragging;
        
        public ToolbarNavigation()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            var fpvIcon = FPV.Q<Icon>();
            fpvIcon.pickingMode = PickingMode.Ignore;
            var fpvManipulator = new FirstPersonViewManipulator(fpvIcon, 0);
            FPV.AddManipulator(fpvManipulator);
            
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            North.RegisterCallback<ClickEvent>(OnNorthClick);
            Perspective.RegisterValueChangedCallback(OnToggleOrthographicView);
            
            FPV.RegisterCallback<PointerEnterEvent>(OnFPVPointerEnter);
            FPV.RegisterCallback<PointerLeaveEvent>(OnFPVPointerLeave);
            fpvManipulator.DragEnded.AddListener(OnDragEnded);
            fpvManipulator.DragStarted.AddListener(OnDragStarted);
        }

        private void OnFPVPointerEnter(PointerEnterEvent evt)
        {
            if(!isDragging)
                PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRAB);
        }
        
        private void OnFPVPointerLeave(PointerLeaveEvent evt)
        {
            if(!isDragging)
                PointerStyle.CancelCursorChange(this);
        }
        
        private void OnDragStarted(Vector2 startPosition)
        {
            isDragging = true;
            PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRABBING);
        } 
        
        private void OnDragEnded(Vector2 endPosition)
        {
            isDragging = false;
            if (worldBound.Contains(endPosition))
                PointerStyle.RequestCursorChange(this, PointerStyle.Style.GRAB); //pointer is still in the panel
            else
                PointerStyle.CancelCursorChange(this);
        }

        private void OnAttachToPanelEvent(AttachToPanelEvent _)
        {
            UpdatePerspectiveIcon();
            schedule.Execute(()=>
            {
                ServiceLocator.GetService<Compass>()?.ChangeDirection.AddListener(UpdateCompass);
            });
        }

        private void OnNorthClick(ClickEvent _)
        {
            ServiceLocator.GetService<Compass>().SwitchToNorth();
        }

        public void UpdateCompass(float yawInDegrees)
        {
            North.Q<Icon>().style.rotate = new StyleRotate(new Rotate(-yawInDegrees));
        }

        private void OnToggleOrthographicView(ChangeEvent<bool> evt)
        {
            ServiceLocator.GetService<CameraService>().ActiveCamera.GetComponent<FreeCamera>()?.EnableOrtographic(evt.newValue);
            UpdatePerspectiveIcon();
        }

        private void UpdatePerspectiveIcon()
        {
            Perspective.Image = Perspective.value ? IconImage.ORTHOGONAL_VIEW : IconImage.PERSPECTIVE_VIEW;
        }
    }
}