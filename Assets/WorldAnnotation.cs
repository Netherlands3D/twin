using System;
using System.Collections.Generic;
using System.Linq;
using GG.Extensions;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI;
using UnityEngine;

namespace Netherlands3D
{
    [RequireComponent(typeof(LayerGameObject))]
    public class WorldAnnotation : MonoBehaviour, ILayerWithPropertyData
    {
        [SerializeField] private TextPopout popoutPrefab;
        private TextPopout annotation;
        private WorldTransform worldTransform;
        private AnnotationPropertyData annotationPropertyData;

        LayerPropertyData ILayerWithPropertyData.PropertyData => annotationPropertyData;

        private void Awake()
        {
            worldTransform = GetComponent<WorldTransform>();
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

        private void OnDestroy()
        {
            annotation.OnEndEdit.AddListener(UpdateProjectData);
            annotationPropertyData.OnDataChanged.RemoveListener(UpdateAnnotation);
        }

        private void UpdateProjectData(string annotationText)
        {
            annotationPropertyData.Data = annotationText;
        }

        private void Update()
        {
            annotation.StickTo(worldTransform.Coordinate);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
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
