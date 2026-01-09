using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    //do not change order for shapetype enum as it is stored in project files
    public enum ShapeType
    {
        Undefined = 0,
        Polygon = 1,
        Line = 2,
        Grid = 3
    }

    public class PolygonCreationService : MonoBehaviour
    {
        public AreaSelection GridInput => gridInput;
        public PolygonInput PolygonInput => polygonInput;
        public PolygonInput LineInput => lineInput;
        
        [SerializeField] private PolygonSelectionLayerGameObject polygonVisualisationPrefab;
        [SerializeField] private AreaSelection gridInput;
        [SerializeField] private PolygonInput polygonInput;
        [SerializeField] private PolygonInput lineInput;

        [SerializeField] private float defaultLineWidth = 10.0f;
        
        private PolygonSelectionService polygonSelectionService;


        private void OnEnable()
        {
            polygonInput.createdNewPolygonArea.AddListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.AddListener(UpdateLayer);

            lineInput.createdNewPolygonArea.AddListener(CreateLineLayer);
            lineInput.editedPolygonArea.AddListener(UpdateLayer);

            gridInput.whenAreaIsSelected.AddListener(CreateOrEditGridLayer);
        }

        private void OnDisable()
        {
            polygonInput.createdNewPolygonArea.RemoveListener(CreatePolygonLayer);
            polygonInput.editedPolygonArea.RemoveListener(UpdateLayer);

            lineInput.createdNewPolygonArea.RemoveListener(CreateLineLayer);
            lineInput.editedPolygonArea.RemoveListener(UpdateLayer);

            gridInput.whenAreaIsSelected.RemoveListener(CreateOrEditGridLayer);
        }

        private void Start()
        {
            polygonSelectionService = ServiceLocator.GetService<PolygonSelectionService>();
        }

        /// <summary>
        /// Enable the proper line or poly input system based on layer type
        /// </summary>
        public void UpdateInputByType(LayerData layer)
        {
            PolygonSelectionLayerPropertyData data = layer.GetProperty<PolygonSelectionLayerPropertyData>();
            EnablePolygonInputByType(data.ShapeType);
            var polygonAsUnityPoints = data.OriginalPolygon.ToUnityPositions().ToList();
            if(data.PolygonBoundingBox == null)
                return;

            switch (data.ShapeType)
            {
                case ShapeType.Polygon: polygonInput.SetPolygon(polygonAsUnityPoints); break;
                case ShapeType.Line: lineInput.SetPolygon(polygonAsUnityPoints); break;
                case ShapeType.Grid: gridInput.SetAreaFromPolygon(polygonAsUnityPoints); break;
                default:
                    Debug.LogError("Polygon shape type undefined, defaulting to PolygonInput");
                    polygonInput.gameObject.SetActive(true);
                    polygonInput.SetPolygon(polygonAsUnityPoints);
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
        
        public void ClearInputs()
        {
            //Clear inputs if no layer is selected by default
            lineInput.ClearPolygon(true);
            polygonInput.ClearPolygon(true);
            gridInput.SetSelectionVisualEnabled(false);
        }

        private void CreatePolygonLayer(List<Vector3> unityPolygon)
        {
            ILayerBuilder layerBuilder = LayerBuilder.Create()
                .OfType(polygonVisualisationPrefab.PrefabIdentifier)
                .NamedAs("Polygon")
                .AddProperty(new PolygonSelectionLayerPropertyData
                {
                    ShapeType = ShapeType.Polygon,
                    OriginalPolygon = unityPolygon.ToCoordinates().ToList()
                });
            var layer = App.Layers.Add(layerBuilder);
            polygonSelectionService.RegisterPolygon(layer.LayerData);
            polygonInput.SetDrawMode(PolygonInput.DrawMode.Edit);
           
        }

        private void UpdateLayer(List<Vector3> editedPolygon)
        {
            polygonSelectionService.ActiveLayer.GetProperty<PolygonSelectionLayerPropertyData>().OriginalPolygon = editedPolygon.ToCoordinates().ToList();
        }

        private void CreateLineLayer(List<Vector3> unityLine)
        {
            ILayerBuilder layerBuilder = LayerBuilder.Create()
                .OfType(polygonVisualisationPrefab.PrefabIdentifier)
                .NamedAs("Line")
                .AddProperty(new PolygonSelectionLayerPropertyData
                {
                    ShapeType = ShapeType.Line,
                    OriginalPolygon = unityLine.ToCoordinates().ToList(),
                    LineWidth = defaultLineWidth
                });
            var layer = App.Layers.Add(layerBuilder);
            polygonSelectionService.RegisterPolygon(layer.LayerData);
            lineInput.SetDrawMode(PolygonInput.DrawMode.Edit);
        }

        //called in the inspector
        public void CreateOrEditGridLayer(Bounds bounds)
        {
            Vector3 bottomLeft = new Vector3(bounds.min.x, 0, bounds.min.z);
            Vector3 topLeft = new Vector3(bounds.min.x, 0, bounds.max.z);
            Vector3 topRight = new Vector3(bounds.max.x, 0, bounds.max.z);
            Vector3 bottomRight = new Vector3(bounds.max.x, 0, bounds.min.z);

            PolygonSelectionLayerPropertyData data = polygonSelectionService.ActiveLayer?.GetProperty<PolygonSelectionLayerPropertyData>();

            //is the current selected layer already a grid and the current input mode is not selected, then we can adjust the polygon
            if (data?.ShapeType == ShapeType.Grid && gridInput.Mode != PolygonInput.DrawMode.Selected)
            {
                var newPolygon = new List<Coordinate>() { new Coordinate(bottomLeft), new Coordinate(topLeft), new Coordinate(topRight), new Coordinate(bottomRight) };
                data.OriginalPolygon = newPolygon;
                return;
            }

            ILayerBuilder layerBuilder = LayerBuilder.Create()
                .OfType(polygonVisualisationPrefab.PrefabIdentifier)
                .NamedAs("Grid")
                .AddProperty(new PolygonSelectionLayerPropertyData
                {
                    ShapeType = ShapeType.Grid,
                    OriginalPolygon = new List<Coordinate>() { new Coordinate(bottomLeft), new Coordinate(topLeft), new Coordinate(topRight), new Coordinate(bottomRight) },
                });
            var layer = App.Layers.Add(layerBuilder);
            polygonSelectionService.RegisterPolygon(layer.LayerData);
            gridInput.SetDrawMode(PolygonInput.DrawMode.Edit);
        }

        public void SetPolygonInputModeToCreate(bool isCreateMode)
        {
            polygonSelectionService.ActiveLayer?.DeselectLayer();

            EnablePolygonInputByType(ShapeType.Polygon);
            polygonInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }

        public void SetLineInputModeToCreate(bool isCreateMode)
        {
            polygonSelectionService.ActiveLayer?.DeselectLayer();

            EnablePolygonInputByType(ShapeType.Line);
            lineInput.SetDrawMode(isCreateMode ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Edit);
        }

        public void SetGridInputModeToCreate(bool active)
        {
            polygonSelectionService.ActiveLayer?.DeselectLayer();

            EnablePolygonInputByType(ShapeType.Grid);
            gridInput.SetDrawMode(active ? PolygonInput.DrawMode.Create : PolygonInput.DrawMode.Selected);
        }

        public void SetGridInputModeToEdit(bool active)
        {
            gridInput.SetDrawMode(active ? PolygonInput.DrawMode.Edit : PolygonInput.DrawMode.Selected);
        }
    }
}