using System;
using System.Collections.Generic;
using System.Linq;
using GG.Extensions;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    public class WorldAnnotation : HierarchicalObjectLayerGameObject, ILayerWithPropertyData
    {
        [SerializeField] private TextPopout popoutPrefab;
        private TextPopout annotation;
        private AnnotationPropertyData annotationPropertyData;

        LayerPropertyData ILayerWithPropertyData.PropertyData => annotationPropertyData;

        protected override void Awake()
        {
            base.Awake();
            annotationPropertyData = new AnnotationPropertyData("");
            CreateTextPopup();
            annotationPropertyData.OnDataChanged.AddListener(UpdateAnnotation);
        }

        private void CreateTextPopup()
        {
            var canvasTransform = FindAnyObjectByType<Canvas>();

            annotation = Instantiate(popoutPrefab, canvasTransform.transform);
            annotation.RectTransform().SetPivot(PivotPresets.BottomCenter);
            annotation.transform.SetSiblingIndex(0);
            annotation.Show(annotationPropertyData.Data, worldTransform.Coordinate, true);
            annotation.ReadOnly = false;
            annotation.OnEndEdit.AddListener(UpdateProjectData);
        }

        protected virtual void OnDestroy()
        {
            base.OnDestroy();
            annotation.OnEndEdit.AddListener(UpdateProjectData);
            annotationPropertyData.OnDataChanged.RemoveListener(UpdateAnnotation);
        }

        private void UpdateProjectData(string annotationText)
        {
            annotationPropertyData.Data = annotationText;
        }

        protected virtual void Update()
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
                    annotationPropertyData.OnDataChanged.RemoveListener(UpdateAnnotation);
                }

                this.annotationPropertyData = annotationProperty; //take existing TransformProperty to overwrite the unlinked one of this class

                UpdateAnnotation(this.annotationPropertyData.Data);

                annotationPropertyData.OnDataChanged.AddListener(UpdateAnnotation);
            }
        }

        private void UpdateAnnotation(string newText)
        {
            annotation.SetTextWithoutNotify(newText);
        }
    }
}
