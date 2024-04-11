using System.Collections.Generic;
using Netherlands3D.SelectionTools;
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

        [Header("Line settings")] 
        [SerializeField] private PolygonInput lineInput;
        [SerializeField] private float defaultLineWidth = 10.0f;
        [SerializeField] private PolygonPropertySection polygonPropertySectionPrefab;
        public static PolygonPropertySection PolygonPropertySectionPrefab { get; private set; }

        private void Awake()
        {
            PolygonPropertySectionPrefab = polygonPropertySectionPrefab;
        }

        private void OnEnable()
        {
            polygonInput.createdNewPolygonArea.AddListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.AddListener(UpdatePolygonLayer);

            lineInput.createdNewPolygonArea.AddListener(CreateLineLayer);
            lineInput.editedPolygonArea.AddListener(UpdateLineLayer);
        }

        public void ShowPolygonVisualisations(bool enabled)
        {
            foreach (var visualisation in layers.Keys)
            {
                visualisation.gameObject.SetActive(enabled);
            }
        }

        private void OnDisable()
        {
            polygonInput.createdNewPolygonArea.RemoveListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.RemoveListener(UpdatePolygonLayer);

            lineInput.createdNewPolygonArea.RemoveListener(CreateLineLayer);
            lineInput.editedPolygonArea.RemoveListener(UpdateLineLayer);
        }

        public void CreatePolygonLayer(List<Vector3> polygon)
        {
            var newObject = new GameObject("Polygon");
            var layerComponent = newObject.AddComponent<PolygonSelectionLayer>();
            layerComponent.Initialize(polygon, polygonExtrusionHeight, polygonMeshMaterial, ShapeType.Polygon);
            layers.Add(layerComponent.PolygonVisualisation, layerComponent);
            layerComponent.polygonSelected.AddListener(ProcessPolygonSelection);
            
            activeLayer = layerComponent;
            polygonInput.SetDrawMode(PolygonInput.DrawMode.Edit); //set the mode to edit explicitly, so the reselect functionality of ProcessPolygonSelection() will immediately work
        }
        public void UpdatePolygonLayer(List<Vector3> editedPolygon)
        {
            activeLayer.SetPolygon(editedPolygon);
        }

        public void CreateLineLayer(List<Vector3> line)
        {
            var newObject = new GameObject("Line");
            var layerComponent = newObject.AddComponent<PolygonSelectionLayer>();
            layerComponent.Initialize(line, polygonExtrusionHeight, polygonMeshMaterial, ShapeType.Line, defaultLineWidth);
            layers.Add(layerComponent.PolygonVisualisation, layerComponent);
            layerComponent.polygonSelected.AddListener(ProcessPolygonSelection);
            
            activeLayer = layerComponent;
            lineInput.SetDrawMode(PolygonInput.DrawMode.Edit); //set the mode to edit explicitly, so the reselect functionality of ProcessPolygonSelection() will immediately work
        }
        public void UpdateLineLayer(List<Vector3> editedLine)
        {
            activeLayer.SetLine(editedLine);
        }

        private void ProcessPolygonSelection(PolygonSelectionLayer layer)
        {
            //Do not allow selecting a new polygon if we are still creating one
            if (polygonInput.Mode == PolygonInput.DrawMode.Create || lineInput.Mode == PolygonInput.DrawMode.Create)
                return;

            activeLayer = layer;
            if (layer)
            {
                if(layer.ShapeType == ShapeType.Polygon)
                {
                    polygonInput.gameObject.SetActive(true);
                    lineInput.gameObject.SetActive(false);
                    polygonInput.ReselectPolygon(layer.OriginalPolygon);
                }
                else if(layer.ShapeType == ShapeType.Line)
                {
                    lineInput.gameObject.SetActive(true);
                    polygonInput.gameObject.SetActive(false);
                    lineInput.ReselectPolygon(layer.OriginalPolygon);
                }
                return;
            }
            
            //Clear inputs if no layer is selected by default
            var emptyList = new List<Vector3>();
            polygonInput.ReselectPolygon(emptyList);
            lineInput.ReselectPolygon(emptyList);
        }

        public void SetPolygonInputModeToCreate(bool isCreateMode)
        {
            if(activeLayer)
                activeLayer.DeselectPolygon(); 
            
            polygonInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }

        public void SetLineInputModeToCreate(bool isCreateMode)
        {
            if(activeLayer)
                activeLayer.DeselectPolygon();

            lineInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }
    }
}