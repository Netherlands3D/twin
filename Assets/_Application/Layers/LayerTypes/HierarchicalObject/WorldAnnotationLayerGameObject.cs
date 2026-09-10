using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class WorldAnnotationLayerGameObject : HierarchicalObjectLayerGameObject, IVisualizationWithWorldUI
    {
        public VisualElement GetVisualElement()
        {
            return annotation.element;
        }
        
        private AnnotationTextObject annotation;
        private const float offsetPixels = 50; //todo make this from uss instead
        
        //set the Bbox to 10x10 meters to make the jump to object functionality work.
        public override BoundingBox Bounds => new BoundingBox(new Coordinate(transform.position - 5 * Vector3.one), new Coordinate(transform.position + 5 * Vector3.one));

        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            //create world text object with WorldTransform.Coordinate as cached coordinate so we dont need to use update
            AnnotationBehaviour behaviour = ServiceLocator.GetService<ContextMenuBehaviour>().GetBehaviour<AnnotationBehaviour>();
            annotation = behaviour.AddWorldTextObject("testing", WorldTransform.Coordinate, WorldText.SnappingSide.Above, offsetPixels);
          
        }
        
        private void OnDestroy()
        {
            //remove annotation from worldtexts
            AnnotationBehaviour behaviour = ServiceLocator.GetService<ContextMenuBehaviour>().GetBehaviour<AnnotationBehaviour>();
           
            behaviour.RemoveWorldTextObject(annotation);
        }

        private void OnEditChanged(bool isEditing)
        {
            if(isEditing)
                ClearTransformHandles();
            else
                SetPropertyDataText(annotation.element.Text);
        }
      
        public override void ApplyStyling()
        {
            base.ApplyStyling();
            LayerFeature feature = CreateFeature(annotation);
            Symbolizer styling = GetStyling(feature);
            var fillColor = styling.GetFillColor();
            if (fillColor.HasValue)
                annotation.SetColor(fillColor.Value);
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
            annotation.element.SetText(annotationText);
        }
        
        private void OnClickAnnotation(ClickEvent e)
        {
            ServiceLocator.GetService<SelectionService>().SelectVisualisation(this);
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            var property = LayerData.GetProperty<TransformLayerPropertyData>();
            property.OnPositionChanged.AddListener(OnUpdateAnnotationPosition);
            
            annotation.AddTextEditListener(OnEditChanged);
            annotation.element.RegisterCallback<ClickEvent>(OnClickAnnotation);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            var property = LayerData.GetProperty<TransformLayerPropertyData>();
            property.OnPositionChanged.RemoveListener(OnUpdateAnnotationPosition);
            
            annotation.RemoveTextEditListener(OnEditChanged);
            annotation.element.UnregisterCallback<ClickEvent>(OnClickAnnotation);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            base.OnLayerActiveInHierarchyChanged(isActive);
            annotation.SetVisible(isActive);
        }

        private void OnUpdateAnnotationPosition(Coordinate coordinate)
        {
            annotation.coordinate = coordinate;
        }
    }
}
