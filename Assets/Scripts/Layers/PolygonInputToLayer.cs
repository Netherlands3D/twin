using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Events;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class PolygonInputToLayer : MonoBehaviour
    {
        public static PolygonInputToLayer Instance;

        [Header("Polygon settings")] [SerializeField]
        private float polygonExtrusionHeight = 0.1f;

        [SerializeField] private Material polygonMeshMaterial;

        private Dictionary<PolygonVisualisation, PolygonSelectionLayer> layers = new();
        private PolygonSelectionLayer activeLayer;

        [SerializeField] private PolygonInput polygonInput;
        [SerializeField] private BoolEvent enablePolygonInputEvent;
        [SerializeField] private Vector3ListEvent polygonCreatedEvent;
        [SerializeField] private Vector3ListEvent polygonEditedEvent;

        public PolygonInput PolygonInput => polygonInput;
        
        private void Awake()
        {
            if (Instance)
            {
                Debug.LogError("An Instance of PolygonInputLayer already exists. There should only be one PolygonInputToLayer object", gameObject);
                return;
            }

            Instance = this;
        }

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
            layerComponent.polygonSelected.AddListener(ReselectPolygon);

            activeLayer = layerComponent;
        }

        public void EnablePolygonInput(bool isOn)
        {
            enablePolygonInputEvent.InvokeStarted(isOn);
            // polygonInput.gameObject.SetActive(isOn);
        }

        private void ReselectPolygon(PolygonSelectionLayer layer)
        {
            EnablePolygonInput(layer != null);
            activeLayer = layer;
            if (layer)
                polygonInput.ReselectPolygon(layer.Polygon.SolidPolygon.ToVector3List());
        }
        
        public void UpdateLayer(List<Vector3> editedPolygon)
        {
            activeLayer.SetPolygon(editedPolygon);
        }
    }
}