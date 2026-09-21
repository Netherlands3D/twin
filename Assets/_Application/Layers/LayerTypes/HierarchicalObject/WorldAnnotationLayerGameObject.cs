using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class WorldAnnotationLayerGameObject : HierarchicalObjectLayerGameObject, IVisualizationWithWorldUI
    {
        private SelectionService selectionService;
        private InputService inputService;
        private WorldUIService worldUIService;
        private CameraService cameraService;
        private AppRootBehaviour appRootBehaviour;
        private WorldText worldTextElement; 
        private FloatingElement floatingElement;

        public VisualElement VisualElement => worldTextElement;

        private const float MaxPixelDistanceOffset = 100;
        private const float worldSpaceOffset = 10;
        
        //set the Bbox to 10x10 meters to make the jump to object functionality work.
        public override BoundingBox Bounds => new BoundingBox(new Coordinate(transform.position - 5 * Vector3.one), new Coordinate(transform.position + 5 * Vector3.one));

        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            inputService = ServiceLocator.GetService<InputService>();
            selectionService = ServiceLocator.GetService<SelectionService>();
            worldUIService  = ServiceLocator.GetService<WorldUIService>();
            cameraService = App.Cameras;
            appRootBehaviour = App.UIRoot;
            InitializeWorldUI();
        }

        private void InitializeWorldUI()
        {
            floatingElement = new FloatingElement();
            worldUIService.AddToFloatingElementsContent(floatingElement);
            worldTextElement = new WorldText("");
            worldTextElement.SetSnappingSide(WorldText.SnappingSide.Above);
            floatingElement.Add(worldTextElement);
        }

        private void OnEditChanged(bool isEditing)
        {
            if(isEditing)
                ClearTransformHandles();
            else
            {
                AnnotationPropertyData annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();
                annotationPropertyData.AnnotationText = worldTextElement.Text;
            }
            
            inputService.SetCameraActionsEnabled(!isEditing);
        }
        
        public override void ApplyStyling()
        {
            base.ApplyStyling();
            LayerFeature feature = CreateFeature(worldTextElement);
            Symbolizer styling = GetStyling(feature);
            var fillColor = styling.GetFillColor();
            if (fillColor.HasValue)
                worldTextElement.SetColor(fillColor.Value);
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            base.LoadProperties(properties);
            InitProperty<AnnotationPropertyData>(properties, null, "");
        }

        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();
            AnnotationPropertyData annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();
            worldTextElement.SetText(annotationPropertyData.AnnotationText);
        }
        
        private void OnClickAnnotation(PointerDownEvent e)
        {
            selectionService.SelectVisualisation(this);
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            worldTextElement.NameField.OnEditingChanged.AddListener(OnEditChanged);
            worldTextElement.RegisterCallback<PointerDownEvent>(OnClickAnnotation);
            
            var annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();
            annotationPropertyData.OnAnnotationTextChanged.AddListener(worldTextElement.SetText);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            worldTextElement.NameField.OnEditingChanged.RemoveListener(OnEditChanged);
            worldTextElement.UnregisterCallback<PointerDownEvent>(OnClickAnnotation);
            
            var annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();
            annotationPropertyData.OnAnnotationTextChanged.RemoveListener(worldTextElement.SetText);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            base.OnLayerActiveInHierarchyChanged(isActive);
            SetVisible(isActive);
        }
        
        public void SetVisible(bool visible)
        {
            worldTextElement.EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        }

        protected override void Update()
        {
            base.Update();
            Vector3 worldPos = WorldTransform.Coordinate.ToUnity();
            var screenPos =  cameraService.ActiveCamera.WorldToScreenPoint(worldPos);
            Vector2 panelPos = appRootBehaviour.GetUIPositionFromScreenPosition(screenPos);
            var localPos = worldUIService.FloatingElementsContent.WorldToLocal(panelPos);
            floatingElement.SetPosition(localPos);

            var offsetScreenPos = cameraService.ActiveCamera.WorldToScreenPoint(worldPos + Vector3.right * worldSpaceOffset);
            float dist = Mathf.Abs(offsetScreenPos.x - screenPos.x);
            float t = Mathf.InverseLerp(1500f, 0f, cameraService.ActiveCamera.transform.position.y);
            float pixelOffset = Mathf.Min(dist * t, MaxPixelDistanceOffset);
            worldTextElement.SetLabelOffset(pixelOffset);
        }
        
        private void OnDestroy()
        {
            worldUIService.RemoveFromFloatingElementsContent(floatingElement);
            floatingElement = null;
        }
    }
}
