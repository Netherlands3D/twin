using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Services;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public enum ShapeType
    {
        Undefined = 0,
        Polygon = 1,
        Line = 2,
        Grid = 3
    }

    public class PolygonInputToLayer : MonoBehaviour
    {
        [SerializeField] private PolygonSelectionVisualisation polygonSelectionVisualisationPrefab;
        private Dictionary<PolygonSelectionVisualisation, LayerData> layers = new();

        private LayerData activeLayer;

        private LayerData ActiveLayer
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
                PolygonSelectionLayerPropertyData propertyData = layer.GetProperty<PolygonSelectionLayerPropertyData>();
                if (propertyData == null) continue;

                //TODO is this the right way to get the polygon visualisation layergameobject?
                var match = FindObjectsByType<PolygonSelectionVisualisation>(FindObjectsSortMode.None).ToList()
                .FirstOrDefault(v => v.LayerData == layer);

                //TODO after refactoring the layersystem and onreferencechanged this should also be refactored! 
                propertyData.polygonSelected.AddListener(ProcessPolygonSelection);
                UnityAction referenceListener = null;
                referenceListener = () =>
                {
                    layers.Add(match, layer);

                    if (!propertyData.IsMask)
                        match.SetVisualisationActive(enabled);

                    layer.OnPrefabIdChanged.RemoveListener(referenceListener);
                };
                layer.OnPrefabIdChanged.AddListener(referenceListener);
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

        private void ProcessPolygonSelection(LayerData layer)
        {
            PolygonSelectionLayerPropertyData data = layer?.GetProperty<PolygonSelectionLayerPropertyData>();
            //we don't reselect immediately in case of a grid, but we already register the active layer
            if (data?.ShapeType == ShapeType.Grid)
            {
                ActiveLayer = layer;
                ReselectInputByType(layer);
                gridInput.SetSelectionVisualEnabled(true);
                return;
            }

            //Do not allow selecting a new polygon if we are still creating one
            if (polygonInput.Mode == PolygonInput.DrawMode.Create || lineInput.Mode == PolygonInput.DrawMode.Create)
                return;

            ClearSelection();

            ActiveLayer = layer;
            ReselectLayerPolygon(layer);
        }

        private void ReselectLayerPolygon(LayerData layer)
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
        private void ReselectInputByType(LayerData layer)
        {
            PolygonSelectionLayerPropertyData data = layer.GetProperty<PolygonSelectionLayerPropertyData>();
            EnablePolygonInputByType(data.ShapeType);
            var polygonAsUnityPoints = data.OriginalPolygon.ToUnityPositions().ToList();
            
            switch (data.ShapeType)
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
            lineInput.ClearPolygon(true);
            polygonInput.ClearPolygon(true);
            gridInput.SetSelectionVisualEnabled(false);
        }

        public void ShowPolygonVisualisations(bool enabled)
        {
            foreach (var polygonLayer in layers.Values)
            {
                PolygonSelectionLayerPropertyData propertyData = polygonLayer.GetProperty<PolygonSelectionLayerPropertyData>();
                if (propertyData.IsMask)
                {
                    continue;
                }
                var key = layers.FirstOrDefault(x => x.Value == polygonLayer).Key;
                key.gameObject.SetActive(enabled);
            }

            selectionCalculator.gameObject.SetActive(enabled);
        }

        private void CreatePolygonLayer(List<Vector3> unityPolygon)
        {
            //todo: this is now broken, needs to be fixed
            //PolygonSelectionLayer data = new PolygonSelectionLayer( 
            //    "Polygon", 
            //    polygonSelectionVisualisationPrefab.PrefabIdentifier, 
            //    unityPolygon, 
            //    ShapeType.Polygon                
            //);

            //// Layer layer = App.Layers.VisualizeData(data);
            //layers.Add(data.PolygonVisualisation, data);
            //data.polygonSelected.AddListener(ProcessPolygonSelection);
            //polygonInput.SetDrawMode(PolygonInput.DrawMode.Edit); //set the mode to edit explicitly, so the reselect functionality of ProcessPolygonSelection() will immediately work
        }

        private void UpdateLayer(List<Vector3> editedPolygon)
        {
            //TODO is this the right way to get the polygon visualisation layergameobject?
            var match = FindObjectsByType<PolygonSelectionVisualisation>(FindObjectsSortMode.None).ToList()
            .FirstOrDefault(v => v.LayerData == ActiveLayer);
            match.SetShape(editedPolygon.ToCoordinates().ToList());
        }

        private void CreateLineLayer(List<Vector3> unityLine)
        {
            //TODO rewrite this to work
            //_ = new PolygonSelectionLayer(
            //    "Line", 
            //    polygonSelectionVisualisationPrefab.PrefabIdentifier, 
            //    unityLine, 
            //    ShapeType.Line, 
            //    defaultLineWidth,
            //    layer =>
            //    {                  
            //        if (layer is not PolygonSelectionLayer polygonSelectionLayer) return;
            //        layers.Add(polygonSelectionLayer.PolygonVisualisation, polygonSelectionLayer);
            //        polygonSelectionLayer.polygonSelected.AddListener(ProcessPolygonSelection);
            //        lineInput.SetDrawMode(PolygonInput.DrawMode.Edit); //set the mode to edit explicitly, so the reselect functionality of ProcessPolygonSelection() will immediately work                   
            //    }
            //);
        }

        //called in the inspector
        public void CreateOrEditGridLayer(Bounds bounds)
        {
            Vector3 bottomLeft = new Vector3(bounds.min.x, 0, bounds.min.z);
            Vector3 topLeft = new Vector3(bounds.min.x, 0, bounds.max.z);
            Vector3 topRight = new Vector3(bounds.max.x, 0, bounds.max.z);
            Vector3 bottomRight = new Vector3(bounds.max.x, 0, bounds.min.z);

            PolygonSelectionLayerPropertyData data = ActiveLayer?.GetProperty<PolygonSelectionLayerPropertyData>();

            //is the current selected layer already a grid and the current input mode is not selected, then we can adjust the polygon
            if (data?.ShapeType == ShapeType.Grid && gridInput.Mode != PolygonInput.DrawMode.Selected)
            {
                //TODO is this the right way to get the polygon visualisation layergameobject?
                var match = FindObjectsByType<PolygonSelectionVisualisation>(FindObjectsSortMode.None).ToList()
                .FirstOrDefault(v => v.LayerData == ActiveLayer);
                match.SetShape(new List<Coordinate>() { new Coordinate(bottomLeft), new Coordinate(topLeft), new Coordinate(topRight), new Coordinate(bottomRight) });
                return;
            }

            ILayerBuilder layerBuilder = LayerBuilder.Create()
                .OfType(polygonSelectionVisualisationPrefab.PrefabIdentifier)
                .NamedAs("Grid")
                .AddProperty(new PolygonSelectionLayerPropertyData{ 
                    ShapeType = ShapeType.Grid,
                    OriginalPolygon = new List<Coordinate>() { new Coordinate(bottomLeft), new Coordinate(topLeft), new Coordinate(topRight), new Coordinate(bottomRight) },                    
                });
            App.Layers.Add(layerBuilder, visualisation =>
            {
                PolygonSelectionVisualisation polygonVisualisation = visualisation as PolygonSelectionVisualisation;
                layers.Add(polygonVisualisation, visualisation.LayerData);
                PolygonSelectionLayerPropertyData data = visualisation.LayerData.GetProperty<PolygonSelectionLayerPropertyData>();
                data.polygonSelected.AddListener(ProcessPolygonSelection);
                gridInput.SetDrawMode(PolygonInput.DrawMode.Edit);
                ProcessPolygonSelection(visualisation.LayerData);
            });
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

            EnablePolygonInputByType(ShapeType.Grid);
            gridInput.SetDrawMode(active ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Selected);
        }

        public void SetGridInputModeToEdit(bool active)
        {
            gridInput.SetDrawMode(active ? PolygonInput.DrawMode.Edit : PolygonInput.DrawMode.Selected);
        }
    }
}