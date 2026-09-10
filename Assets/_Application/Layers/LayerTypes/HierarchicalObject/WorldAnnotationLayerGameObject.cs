using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
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
        [SerializeField] private bool debug = false;
        [SerializeField] private Material debugMaterial;
        
        private SelectionService selectionService;
        private InputService inputService;
        private ContextMenuBehaviour contextMenuBehaviour;
        private GameObject testObject = null;
        public WorldText element;
        public FloatingElement floatingElement;
        
        public VisualElement GetVisualElement()
        {
            return element;
        }
        
        //private AnnotationTextObject annotation;
        private const float offsetPixels = 50; //todo make this from uss instead
        
        //set the Bbox to 10x10 meters to make the jump to object functionality work.
        public override BoundingBox Bounds => new BoundingBox(new Coordinate(transform.position - 5 * Vector3.one), new Coordinate(transform.position + 5 * Vector3.one));

        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            inputService = ServiceLocator.GetService<InputService>();
            selectionService = ServiceLocator.GetService<SelectionService>();
            contextMenuBehaviour  = ServiceLocator.GetService<ContextMenuBehaviour>();
            InitializeWorldUI();
        }

        private void InitializeWorldUI()
        {
            FloatingElement floatingElement = new FloatingElement();
            contextMenuBehaviour.AddToFloatingElementsContent(floatingElement);
           
            this.floatingElement = floatingElement;
            if(floatingElement == null)
                throw new Exception("FloatingElement is missing");
            
            element = new WorldText();
            element.SetText("");
            element.SetSnappingSide(WorldText.SnappingSide.Above);
            element.SetLabelOffset(offsetPixels);
            floatingElement.Add(element);
        }

        private void OnEditChanged(bool isEditing)
        {
            if(isEditing)
                ClearTransformHandles();
            else
                SetPropertyDataText(element.Text);
            
            inputService.SetCameraActionsEnabled(!isEditing);
        }
        
        public override void ApplyStyling()
        {
            base.ApplyStyling();
            LayerFeature feature = CreateFeature(element);
            Symbolizer styling = GetStyling(feature);
            var fillColor = styling.GetFillColor();
            if (fillColor.HasValue)
                element.SetColor(fillColor.Value);
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
            SetPropertyDataText(annotationPropertyData.AnnotationText);
        }
        
        private void SetPropertyDataText(string annotationText)
        {
            var annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();
            annotationPropertyData.AnnotationText = annotationText;
            element.SetText(annotationText);
        }
        
        private void OnClickAnnotation(PointerDownEvent e)
        {
            selectionService.SelectVisualisation(this);
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            element.NameField.OnEditingChanged.AddListener(OnEditChanged);
            element.RegisterCallback<PointerDownEvent>(OnClickAnnotation);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            element.NameField.OnEditingChanged.RemoveListener(OnEditChanged);
            element.UnregisterCallback<PointerDownEvent>(OnClickAnnotation);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            base.OnLayerActiveInHierarchyChanged(isActive);
            SetVisible(isActive);
        }
        
        public void SetVisible(bool visible)
        {
            element.EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        }

        protected override void Update()
        {
            base.Update();
            var screenPos =  App.Cameras.ActiveCamera.WorldToScreenPoint(WorldTransform.Coordinate.ToUnity());
            Vector2 panelPos = App.UIRoot.GetUIPositionFromScreenPosition(screenPos);
            var contentPos = contextMenuBehaviour.FloatingElementsContent.worldBound.position;
            var localPos = panelPos - contentPos;
            floatingElement.SetPosition(localPos);
            
            if(debug)
            {
                if (testObject == null)
                {
                    testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Destroy(testObject.GetComponent<Collider>());

                    testObject.transform.localScale = new Vector3(10f, 10f, 10f);
                    MeshRenderer meshRenderer = testObject.GetComponent<MeshRenderer>();
                    meshRenderer.material = debugMaterial;
                }
                testObject.transform.position = WorldTransform.Coordinate.ToUnity();
            }
        }
        
        private void OnDestroy()
        {
            contextMenuBehaviour.RemoveFromFloatingElementsContent(floatingElement);
            floatingElement = null;
        }
    }
}
