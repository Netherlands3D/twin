using System.Collections.Generic;
using System.Linq;
using GG.Extensions;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Tools;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class WorldAnnotationLayerGameObject : HierarchicalObjectLayerGameObject
    {
        [SerializeField] private TextPopout popoutPrefab;
        [SerializeField] private Tool layerTool;

        private TextPopout annotation;
        private AnnotationPropertyData annotationPropertyData => (AnnotationPropertyData)transformPropertyData;
        private enum EditMode
        {
            Disabled, // Neither move the annotation, nor edit the text
            Move, // Move the annotation in the world, but don't edit the text
            TextEdit // Edit the text of the annotation, do not move the annotation
        }
        private EditMode mode = EditMode.Disabled;
        
        //set the Bbox to 10x10 meters to make the jump to object functionality work.
        public override BoundingBox Bounds => new BoundingBox(new Coordinate(transform.position - 5 * Vector3.one), new Coordinate(transform.position + 5 * Vector3.one));

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
            annotation.Show(annotationPropertyData.AnnotationText, WorldTransform.Coordinate, true);
            annotation.ReadOnly = !layerTool.Open;
            annotation.OnEndEdit.AddListener(SetPropertyDataText);
            annotation.TextFieldSelected.AddListener(OnAnnotationSelected); // avoid transform handles from being able to move the annotation when trying to select text
            annotation.TextFieldDoubleClicked.AddListener(OnAnnotationDoubleClicked);
        }

        private void OnAnnotationSelected()
        {
            if(!layerTool.Open)
                return;
            
            SetEditMode(EditMode.Move);
        }

        private void OnAnnotationDoubleClicked()
        {
            if (!layerTool.Open)
            {
                layerTool.OpenInspector();
                SetEditMode(EditMode.Move);
            }
            else
            {
                SetEditMode(EditMode.TextEdit);
            }
        }

        private void SetEditMode(EditMode newMode)
        {
            mode = newMode;
            switch (mode)
            {
                case EditMode.Disabled:
                    annotation.ReadOnly = true;
                    LayerData.DeselectLayer();
                    break;    
                case EditMode.Move:
                    annotation.ReadOnly = true;
                    LayerData.SelectLayer(true);
                    break;
                case EditMode.TextEdit:
                    annotation.ReadOnly = false;
                    LayerData.SelectLayer(true);
                    ClearTransformHandles();
                    break;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            annotation.OnEndEdit.RemoveListener(SetPropertyDataText);
            annotationPropertyData.OnAnnotationTextChanged.RemoveListener(UpdateAnnotation);
            annotation.TextFieldSelected.RemoveListener(OnDeselect);
            annotation.TextFieldDoubleClicked.RemoveListener(OnAnnotationDoubleClicked);

            Destroy(annotation.gameObject);
        }

        private void SetPropertyDataText(string annotationText)
        {
            annotationPropertyData.AnnotationText = annotationText;
        }

        protected override void Update()
        {
            base.Update();
            annotation.StickTo(WorldTransform.Coordinate);
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

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            base.OnLayerActiveInHierarchyChanged(isActive);
            annotation.gameObject.SetActive(isActive);
        }
    }
}
