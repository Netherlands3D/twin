using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public class PolygonInputToLayer : MonoBehaviour
    {
        private const string prefabId = "0dd48855510674827b667fa4abd5cf60";
        private Dictionary<PolygonSelectionVisualisation, PolygonSelectionLayer> layers = new();

        private PolygonSelectionLayer activeLayer;

        private PolygonSelectionLayer ActiveLayer
        {
            get { return activeLayer; }
            set { activeLayer = value; }
        }

        [SerializeField] private PolygonInput polygonInput;

        [Header("Line settings")] 
        [SerializeField] private PolygonInput lineInput;
        [SerializeField] private float defaultLineWidth = 10.0f;
        [SerializeField] private PolygonPropertySection polygonPropertySectionPrefab;

        [Header("Grid Settings")]
        [SerializeField] private AreaSelection gridInput;

        public static PolygonPropertySection PolygonPropertySectionPrefab { get; private set; }

        private void Awake()
        {
            PolygonPropertySectionPrefab = polygonPropertySectionPrefab;
        }

        private void OnEnable()
        {
            polygonInput.createdNewPolygonArea.AddListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.AddListener(UpdateLayer);

            lineInput.createdNewPolygonArea.AddListener(CreateLineLayer);
            lineInput.editedPolygonArea.AddListener(UpdateLayer);

            gridInput.whenAreaIsSelected.AddListener(CreateOrEditGridLayer);

            ProjectData.Current.OnDataChanged.AddListener(ReregisterAllPolygons);
        }

        private void ReregisterAllPolygons(ProjectData newData)
        {
            layers.Clear();

            foreach (var layer in newData.RootLayer.ChildrenLayers)
            {
                if (layer is PolygonSelectionLayer polygon)
                {
                    polygon.polygonSelected.AddListener(ProcessPolygonSelection);
                    layers.Add(polygon.PolygonVisualisation, polygon);
                }
            }

            ShowPolygonVisualisations(false); //disable the visualisations, because the layer panel is not opened
        }

        private void OnDisable()
        {
            polygonInput.createdNewPolygonArea.RemoveListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.RemoveListener(UpdateLayer);

            lineInput.createdNewPolygonArea.RemoveListener(CreateLineLayer);
            lineInput.editedPolygonArea.RemoveListener(UpdateLayer);
            
            gridInput.whenAreaIsSelected.RemoveListener(CreateOrEditGridLayer);
        }

        private void ProcessPolygonSelection(PolygonSelectionLayer layer)
        {
            //we don't reselect immediately in case of a grid, but we already register the active layer
            if (layer?.ShapeType == ShapeType.Grid) 
            {
                ClearSelection();

                ActiveLayer = layer;
                return;
            }
            
            //Do not allow selecting a new polygon if we are still creating one
            if (polygonInput.Mode == PolygonInput.DrawMode.Create || lineInput.Mode == PolygonInput.DrawMode.Create)
                return;

            ClearSelection();

            ActiveLayer = layer;
            ReselectLayerPolygon(layer);
        }

        private void ReselectLayerPolygon(PolygonSelectionLayer layer)
        {
            if (layer == null)
            {
                // reselecting nothing, disabling all polygon selections
                polygonInput.gameObject.SetActive(false);
                lineInput.gameObject.SetActive(false);
                gridInput.gameObject.SetActive(false);

                return;
            }
        
            //Align the input sytem by reselecting using layer polygon
            ReselectInputByType(layer);
        }

        /// <summary>
        /// Enable the proper line or poly input system based on layer type
        /// </summary>
        private void ReselectInputByType(PolygonSelectionLayer layer)
        {
            EnablePolygonInputByType(layer.ShapeType);
            switch (layer.ShapeType)
            {
                case ShapeType.Polygon:
                    polygonInput.ReselectPolygon(layer.GetPolygonAsUnityPoints());
                    break;
                case ShapeType.Line:
                    lineInput.ReselectPolygon(layer.GetPolygonAsUnityPoints());
                    break;
                case ShapeType.Grid:
                    gridInput.ReselectAreaFromPolygon(layer.GetPolygonAsUnityPoints());
                    break;
                default:
                    Debug.LogError("Polygon shape type undefined, defaulting to PolygonInput");
                    polygonInput.gameObject.SetActive(true);
                    polygonInput.ReselectPolygon(layer.GetPolygonAsUnityPoints());
                    break;
            }
        }

        private void EnablePolygonInputByType(ShapeType type)
        {
            switch (type)
            {
                case ShapeType.Undefined:
                    break;
                case ShapeType.Polygon:
                    polygonInput.gameObject.SetActive(true);
                    break;
                case ShapeType.Line:
                    lineInput.gameObject.SetActive(true);
                    break;
                case ShapeType.Grid:
                    gridInput.gameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public void ClearSelection()
        {
            //Clear inputs if no layer is selected by default
            var emptyList = new List<Vector3>();
            polygonInput.ReselectPolygon(emptyList);
            lineInput.ReselectPolygon(emptyList);
            gridInput.ClearSelection();
        }

        public void ShowPolygonVisualisations(bool enabled)
        {
            foreach (var visualisation in layers.Keys)
            {
                visualisation.gameObject.SetActive(enabled);
            }
        }

        private void CreatePolygonLayer(List<Vector3> unityPolygon)
        {
            var layer = new PolygonSelectionLayer("Polygon", prefabId, unityPolygon, ShapeType.Polygon);
            layers.Add(layer.PolygonVisualisation, layer);
            layer.polygonSelected.AddListener(ProcessPolygonSelection);
            polygonInput.SetDrawMode(PolygonInput.DrawMode.Edit); //set the mode to edit explicitly, so the reselect functionality of ProcessPolygonSelection() will immediately work
            ProcessPolygonSelection(layer);
        }

        private void UpdateLayer(List<Vector3> editedPolygon)
        {
            var coordinates = PolygonSelectionLayer.ConvertToCoordinates(editedPolygon);
            ActiveLayer.SetShape(coordinates);
        }

        private void CreateLineLayer(List<Vector3> unityLine)
        {
            // var visualisation = Instantiate(polygonVisualisationPrefab);
            var layer = new PolygonSelectionLayer("Line", prefabId, unityLine, ShapeType.Line, defaultLineWidth);
            layers.Add(layer.PolygonVisualisation, layer);
            layer.polygonSelected.AddListener(ProcessPolygonSelection);
            lineInput.SetDrawMode(PolygonInput.DrawMode.Edit); //set the mode to edit explicitly, so the reselect functionality of ProcessPolygonSelection() will immediately work
            ProcessPolygonSelection(layer);
        }

        //called in the inspector
        public void CreateOrEditGridLayer(Bounds bounds)
        {
            Vector3 bottomLeft = new Vector3(bounds.min.x, 0, bounds.min.z);
            Vector3 topLeft = new Vector3(bounds.min.x, 0, bounds.max.z);
            Vector3 topRight = new Vector3(bounds.max.x, 0, bounds.max.z);
            Vector3 bottomRight = new Vector3(bounds.max.x, 0, bounds.min.z);

            if (ActiveLayer?.ShapeType == ShapeType.Grid)
            {
                // editing with the grid selection tool redraws the entire polygon
                ActiveLayer.SetShape(new List<Coordinate>() { new Coordinate(bottomLeft), new Coordinate(topLeft), new Coordinate(topRight), new Coordinate(bottomRight) });
                return;
            }

            var layer = new PolygonSelectionLayer("Grid", prefabId, new List<Vector3>() { bottomLeft, topLeft, topRight, bottomRight }, ShapeType.Grid);
            layers.Add(layer.PolygonVisualisation, layer);
            layer.polygonSelected.AddListener(ProcessPolygonSelection);
            ProcessPolygonSelection(layer);
        }


        public void SetPolygonInputModeToCreate(bool isCreateMode)
        {
            ActiveLayer?.DeselectLayer();

            EnablePolygonInputByType(ShapeType.Polygon);
            polygonInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }

        public void SetLineInputModeToCreate(bool isCreateMode)
        {
            ActiveLayer?.DeselectLayer();

            EnablePolygonInputByType(ShapeType.Line);
            lineInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }

        public void SetGridInputModeToCreate(bool active)
        {
            ActiveLayer?.DeselectLayer();
            ActiveLayer = null;
            
            if (active)
                EnablePolygonInputByType(ShapeType.Grid);
            else
                gridInput.gameObject.SetActive(false);
        }
        
        public void SetGridInputModeToEdit()
        {
            EnablePolygonInputByType(ShapeType.Grid);
        }
    }
}