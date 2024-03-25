using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Events;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public class PolygonInputToLayer : MonoBehaviour
    {
        [Header("Polygon settings")] [SerializeField]
        private float polygonExtrusionHeight = 0.1f;

        [SerializeField] private Material polygonMeshMaterial;

        private Dictionary<PolygonVisualisation, PolygonSelectionLayer> layers = new();
        private PolygonSelectionLayer activeLayer;

        [SerializeField] private PolygonInput polygonInput;

        // [SerializeField] private BoolEvent enablePolygonInputEvent;
        [SerializeField] private Vector3ListEvent polygonCreatedEvent;
        [SerializeField] private Vector3ListEvent polygonEditedEvent;

        private void OnEnable()
        {
            polygonCreatedEvent.AddListenerStarted(CreateLayer);
            polygonEditedEvent.AddListenerStarted(UpdateLayer);
        }

        private void OnDisable()
        {
            polygonCreatedEvent.RemoveListenerStarted(CreateLayer);
            polygonEditedEvent.RemoveListenerStarted(UpdateLayer);
        }

        public void CreateLayer(List<Vector3> polygon)
        {
            var newObject = new GameObject("Polygon");
            var layerComponent = newObject.AddComponent<PolygonSelectionLayer>();
            layerComponent.Initialize(polygon, polygonExtrusionHeight, polygonMeshMaterial);
            layers.Add(layerComponent.PolygonVisualisation, layerComponent);
            layerComponent.polygonSelected.AddListener(ProcessPolygonSelection);

            activeLayer = layerComponent;
        }

        private void ProcessPolygonSelection(PolygonSelectionLayer layer)
        {

            if (polygonInput.Mode == PolygonInput.DrawMode.Create)
                return;

            activeLayer = layer;
            if (layer)
                polygonInput.ReselectPolygon(layer.Polygon.SolidPolygon.ToVector3List());
            else
                polygonInput.ReselectPolygon(new List<Vector3>()); //acts as a deselect
        }

        public void UpdateLayer(List<Vector3> editedPolygon)
        {
            activeLayer.SetPolygon(editedPolygon);
        }

        public void SetPolygonInputModeToCreate(bool isCreateMode)
        {
            if(activeLayer)
                activeLayer.DeselectPolygon(); 
            
            polygonInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }
    }
}