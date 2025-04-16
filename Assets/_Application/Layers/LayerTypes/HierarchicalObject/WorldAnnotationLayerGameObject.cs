using System.Collections.Generic;
using System.Linq;
using GG.Extensions;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class WorldAnnotationLayerGameObject : HierarchicalObjectLayerGameObject
    {
        [SerializeField] private TextPopout popoutPrefab;
        private TextPopout annotation;
        private AnnotationPropertyData annotationPropertyData => (AnnotationPropertyData)transformPropertyData;
        
        //set the Bbox to 10x10 meters to make the jump to object functionality work.
        public override BoundingBox Bounds => new BoundingBox(new Coordinate(transform.position - 5 * Vector3.one ), new Coordinate(transform.position + 5 * Vector3.one));

        protected override void Awake()
        {
            base.Awake();
            CreateTextPopup();
            annotationPropertyData.OnAnnotationTextChanged.AddListener(UpdateAnnotation);
        }

        protected override TransformLayerPropertyData InitializePropertyData()
        {
            return new AnnotationPropertyData(new Coordinate(transform.position), transform.eulerAngles, transform.localScale, "");
        }

        private void CreateTextPopup()
        {
            var canvasTransform = FindAnyObjectByType<Canvas>();

            annotation = Instantiate(popoutPrefab, canvasTransform.transform);
            annotation.RectTransform().SetPivot(PivotPresets.BottomCenter);
            annotation.transform.SetSiblingIndex(0);
            annotation.Show(annotationPropertyData.AnnotationText, worldTransform.Coordinate, true);
            annotation.ReadOnly = false;
            annotation.OnEndEdit.AddListener(UpdateProjectData);
            annotation.TextFieldSelected.AddListener(OnDeselect); // avoid transform handles from being able to move the annotation when trying to select text
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            annotation.OnEndEdit.RemoveListener(UpdateProjectData);
            annotationPropertyData.OnAnnotationTextChanged.RemoveListener(UpdateAnnotation);
            annotation.TextFieldSelected.RemoveListener(OnDeselect);

            Destroy(annotation.gameObject);
        }

        private void UpdateProjectData(string annotationText)
        {
            annotationPropertyData.AnnotationText = annotationText;
        }
        
        protected override void Update()
        {
            base.Update();
            annotation.StickTo(worldTransform.Coordinate);
        }

        public override void LoadProperties(List<LayerPropertyData> properties)
        {
            base.LoadProperties(properties);
    
            var annotationProperty = (AnnotationPropertyData)properties.FirstOrDefault(p => p is AnnotationPropertyData);
            if (annotationProperty != null)
            {
                if (annotationPropertyData != null) //unsubscribe events from previous property object, resubscribe to new object at the end of this if block
                {
                    annotationPropertyData.OnAnnotationTextChanged.RemoveListener(UpdateAnnotation);
                }

                transformPropertyData = annotationProperty; //take existing TransformProperty to overwrite the unlinked one of this class

                UpdateAnnotation(this.annotationPropertyData.AnnotationText);

                annotationPropertyData.OnAnnotationTextChanged.AddListener(UpdateAnnotation);
            }
        }

        private void UpdateAnnotation(string newText)
        {
            annotation.SetTextWithoutNotify(newText);
        }
    }
}
