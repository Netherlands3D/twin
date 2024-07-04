using System.Collections.Generic;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
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
        private PolygonSelectionLayer ActiveLayer { 
            get
            {
                return activeLayer;
            }
            set
            {
                activeLayer = value;
            }
        }

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
        
        private void OnDisable()
        {
            polygonInput.createdNewPolygonArea.RemoveListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.RemoveListener(UpdatePolygonLayer);

            lineInput.createdNewPolygonArea.RemoveListener(CreateLineLayer);
            lineInput.editedPolygonArea.RemoveListener(UpdateLineLayer);
        }

        private void ProcessPolygonSelection(PolygonSelectionLayer layer)
        {
            //Do not allow selecting a new polygon if we are still creating one
            if (polygonInput.Mode == PolygonInput.DrawMode.Create || lineInput.Mode == PolygonInput.DrawMode.Create)
                return;

            ClearSelection();

            ActiveLayer = layer;
            if (layer != null)
            {
                ReselectLayerPolygon(layer);
            }    
        }

        private void ReselectLayerPolygon(PolygonSelectionLayer layer)
        {
            Debug.Log("Reselect layer polygon");

            if(layer==null)
                return;

            //Align the input sytem by reselecting using layer polygon
            var inputType = EnableInputByType(layer);
            inputType.ReselectPolygon(layer.OriginalPolygon);
        }

        /// <summary>
        /// Enable the proper line or poly input system based on layer type
        /// </summary>
        private PolygonInput EnableInputByType(PolygonSelectionLayer layer)
        {
            if (layer.ShapeType == ShapeType.Polygon)
            {
                polygonInput.gameObject.SetActive(true);
                lineInput.gameObject.SetActive(false);
                return polygonInput;
            }

            //Default to returning line input
            polygonInput.gameObject.SetActive(false);
            lineInput.gameObject.SetActive(true);
            return lineInput;
        }

        public void ClearSelection()
        {
            //Clear inputs if no layer is selected by default
            var emptyList = new List<Vector3>();
            polygonInput.ReselectPolygon(emptyList);
            lineInput.ReselectPolygon(emptyList);
        }

        public void ShowPolygonVisualisations(bool enabled)
        {
            foreach (var visualisation in layers.Keys)
            {
                visualisation.gameObject.SetActive(enabled);
            }
        }

        public void CreatePolygonLayer(List<Vector3> polygon)
        {
            Debug.Log("------------------------Create polygon layer");
            var layerComponent = new PolygonSelectionLayer();
            layerComponent.CONSTRUCTOR("Polygon");
            layerComponent.Initialize(polygon, polygonExtrusionHeight, polygonMeshMaterial, ShapeType.Polygon);
            layers.Add(layerComponent.PolygonVisualisation, layerComponent);
            layerComponent.polygonSelected.AddListener(ProcessPolygonSelection);
            
            ActiveLayer = layerComponent;
            polygonInput.SetDrawMode(PolygonInput.DrawMode.Edit); //set the mode to edit explicitly, so the reselect functionality of ProcessPolygonSelection() will immediately work
        }
        public void UpdatePolygonLayer(List<Vector3> editedPolygon)
        {
            ActiveLayer.SetShape(editedPolygon);
        }

        public void CreateLineLayer(List<Vector3> line)
        {
            var layerComponent = new PolygonSelectionLayer();
            layerComponent.CONSTRUCTOR("Line");
            layerComponent.Initialize(line, polygonExtrusionHeight, polygonMeshMaterial, ShapeType.Line, defaultLineWidth);
            layers.Add(layerComponent.PolygonVisualisation, layerComponent);
            layerComponent.polygonSelected.AddListener(ProcessPolygonSelection);
            
            ActiveLayer = layerComponent;
            lineInput.SetDrawMode(PolygonInput.DrawMode.Edit); //set the mode to edit explicitly, so the reselect functionality of ProcessPolygonSelection() will immediately work
        }
        public void UpdateLineLayer(List<Vector3> editedLine)
        {
            ActiveLayer.SetShape(editedLine);
        }

        public void SetPolygonInputModeToCreate(bool isCreateMode)
        {
            ActiveLayer?.DeselectLayer();

            polygonInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }

        public void SetLineInputModeToCreate(bool isCreateMode)
        {
            ActiveLayer?.DeselectLayer();

            lineInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }
    }
}