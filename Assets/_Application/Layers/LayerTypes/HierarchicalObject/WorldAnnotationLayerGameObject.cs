using System.Collections.Generic;
using GG.Extensions;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Tools;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class WorldAnnotationLayerGameObject : HierarchicalObjectLayerGameObject
    {
        private AnnotationTextObject annotation;
        
        //set the Bbox to 10x10 meters to make the jump to object functionality work.
        public override BoundingBox Bounds => new BoundingBox(new Coordinate(transform.position - 5 * Vector3.one), new Coordinate(transform.position + 5 * Vector3.one));

        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            //create world text object with WorldTransform.Coordinate as cached coordinate so we dont need to use update
            AnnotationBehaviour behaviour = ServiceLocator.GetService<ContextMenuBehaviour>().GetBehaviour<AnnotationBehaviour>();
            annotation = behaviour.AddWorldTextObject("testing a new piece of text \n and a little bit more", WorldTransform.Coordinate, WorldText.SnappingSide.Above, 0);
        }
        
        private void OnDestroy()
        {
            //remove annotation from worldtexts
            AnnotationBehaviour behaviour = ServiceLocator.GetService<ContextMenuBehaviour>().GetBehaviour<AnnotationBehaviour>();
            behaviour.RemoveWorldTextObject(annotation);
        }
      
        public override void ApplyStyling()
        {
            base.ApplyStyling();
            LayerFeature feature = CreateFeature(annotation);
            Symbolizer styling = GetStyling(feature);
            var fillColor = styling.GetFillColor();
            if (fillColor.HasValue)
                annotation.color = fillColor.Value;
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
            annotation.element.SetText(annotationPropertyData.AnnotationText);
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            var annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();

            //annotation.OnEndEdit.AddListener(SetPropertyDataText);
           // annotation.TextFieldSelected.AddListener(OnAnnotationSelected); // avoid transform handles from being able to move the annotation when trying to select text
            //annotation.TextFieldDoubleClicked.AddListener(OnAnnotationDoubleClicked);
            //annotation.TextFieldInputConfirmed.AddListener(OnAnnotationTextConfirmed);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            var annotationPropertyData = LayerData.GetProperty<AnnotationPropertyData>();

            //annotation.OnEndEdit.RemoveListener(SetPropertyDataText);
            //annotation.TextFieldSelected.RemoveListener(OnAnnotationSelected);
            //annotation.TextFieldDoubleClicked.RemoveListener(OnAnnotationDoubleClicked);
            //annotation.TextFieldInputConfirmed.RemoveListener(OnAnnotationTextConfirmed);
            
            //WorldInteractionBlocker.ClickedOnBlocker.RemoveListener(OnBlockerClicked);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            base.OnLayerActiveInHierarchyChanged(isActive);
        }
    }
}
