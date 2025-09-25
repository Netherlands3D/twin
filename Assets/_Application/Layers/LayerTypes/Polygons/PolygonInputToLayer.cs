using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public class PolygonInputToLayer : MonoBehaviour
    {
        [SerializeField] private PolygonSelectionVisualisation polygonSelectionVisualisationPrefab;
        private Dictionary<PolygonSelectionVisualisation, PolygonSelectionLayer> layers = new();

        private PolygonSelectionLayer activeLayer;

        private PolygonSelectionLayer ActiveLayer
        {
            get { return activeLayer; }
            set { activeLayer = value; }
        }

        [SerializeField] private PolygonSelectionCalculator selectionCalculator;
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
                if (layer is not PolygonSelectionLayer polygon) continue;
                // TODO: Why did I need to add this to stop a project from crashing?
                if (!polygon.PolygonVisualisation) continue;

                polygon.polygonSelected.AddListener(ProcessPolygonSelection);
                layers.Add(polygon.PolygonVisualisation, polygon);
                    
                // Disable the visualisations when loading a project, because the layer panel is not opened.
                // If it is a mask it should not be disabled because we need to render it to get the desired masking effect even if the layer panel is not opened.
                if (polygon.IsMask) continue;

                polygon.SetVisualisationActive(enabled);
            }
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
                // do not call the ReselectLayerPolygon function yet, because we need to wait until the user selects edit mode in the UI
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
            var polygonAsUnityPoints = layer.OriginalPolygon.ToUnityPositions().ToList();
            
            switch (layer.ShapeType)
            {
                case ShapeType.Polygon: polygonInput.ReselectPolygon(polygonAsUnityPoints); break;
                case ShapeType.Line: lineInput.ReselectPolygon(polygonAsUnityPoints); break;
                case ShapeType.Grid: gridInput.ReselectAreaFromPolygon(polygonAsUnityPoints); break;
                default:
                    Debug.LogError("Polygon shape type undefined, defaulting to PolygonInput");
                    polygonInput.gameObject.SetActive(true);
                    polygonInput.ReselectPolygon(polygonAsUnityPoints);
                    break;
            }
        }

        private void EnablePolygonInputByType(ShapeType type)
        {
            switch (type)
            {
                case ShapeType.Undefined: break;
                case ShapeType.Polygon: polygonInput.gameObject.SetActive(true); break;
                case ShapeType.Line: lineInput.gameObject.SetActive(true); break;
                case ShapeType.Grid: gridInput.gameObject.SetActive(true); break;
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
            foreach (var polygonLayer in layers.Values)
            {
                if (polygonLayer.IsMask)
                {
                    continue;
                }

                polygonLayer.PolygonVisualisation.gameObject.SetActive(enabled);
            }

            selectionCalculator.gameObject.SetActive(enabled);
        }

        private void CreatePolygonLayer(List<Vector3> unityPolygon)
        {
            _ = new PolygonSelectionLayer(
                "Polygon", 
                polygonSelectionVisualisationPrefab.PrefabIdentifier, 
                unityPolygon, 
                ShapeType.Polygon,
                onSpawn: data =>
                {
                    if (data is not PolygonSelectionLayer layer) return;

                    layers.Add(layer.PolygonVisualisation, layer);
                    layer.polygonSelected.AddListener(ProcessPolygonSelection);
                    polygonInput.SetDrawMode(PolygonInput.DrawMode.Edit); //set the mode to edit explicitly, so the reselect functionality of ProcessPolygonSelection() will immediately work
                    ProcessPolygonSelection(layer);
                }
            );
            
        }

        private void UpdateLayer(List<Vector3> editedPolygon)
        {
            ActiveLayer.SetShape(editedPolygon.ToCoordinates().ToList());
        }

        private void CreateLineLayer(List<Vector3> unityLine)
        {
            _ = new PolygonSelectionLayer(
                "Line", 
                polygonSelectionVisualisationPrefab.PrefabIdentifier, 
                unityLine, 
                ShapeType.Line, 
                defaultLineWidth,
                layer =>
                {
                    if (layer is not PolygonSelectionLayer polygonSelectionLayer) return;
                    layers.Add(polygonSelectionLayer.PolygonVisualisation, polygonSelectionLayer);
                    polygonSelectionLayer.polygonSelected.AddListener(ProcessPolygonSelection);
                    lineInput.SetDrawMode(PolygonInput.DrawMode.Edit); //set the mode to edit explicitly, so the reselect functionality of ProcessPolygonSelection() will immediately work
                    ProcessPolygonSelection(polygonSelectionLayer);
                }
            );
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
                ActiveLayer.SetShape(new List<Coordinate>() { new Coordinate(bottomLeft), new Coordinate(topLeft), new Coordinate(topRight), new Coordinate(bottomRight) });
                return;
            }
            
            _ = new PolygonSelectionLayer(
                "Grid", 
                polygonSelectionVisualisationPrefab.PrefabIdentifier, 
                new List<Vector3>() { bottomLeft, topLeft, topRight, bottomRight }, 
                ShapeType.Grid,
                onSpawn: data =>
                {
                    if (data is not PolygonSelectionLayer layer) return;

                    layers.Add(layer.PolygonVisualisation, layer);
                    layer.polygonSelected.AddListener(ProcessPolygonSelection);
                    ProcessPolygonSelection(layer);
                }
            );
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

        public void SetGridInputModeToEdit(bool active)
        {
            if (active)
                EnablePolygonInputByType(ShapeType.Grid);
            else
                gridInput.gameObject.SetActive(false);
        }
    }
}