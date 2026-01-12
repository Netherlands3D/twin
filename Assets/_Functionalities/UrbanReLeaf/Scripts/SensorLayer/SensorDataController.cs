using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Functionalities.UrbanReLeaf
{
    public abstract class SensorDataController : MonoBehaviour, IVisualizationWithPropertyData
    {
        public bool StaticSensorData = false;
        public enum SensorPropertyType { None, Temperature, RelativeHumidity, ThermalDiscomfort }
        public SensorPropertyType propertyType;
        public List<SensorCell> Cells { get { return StaticSensorData ? staticCells : cells; } }
        
        public float Maximum;
        public float Minimum;        
        public Color MaxColor;
        public Color MinColor;
        public float defaultMinValue;
        public float defaultMaxValue;
        public Color defaultMinColor;
        public Color defaultMaxColor;
        protected DateTime startDate;
        protected DateTime endDate;
        protected DateTime defaultStartDate = new();
        protected DateTime defaultEndDate = new();
        
        public float heightMultiplier = 500; //how high will the tile rise on selection, multiplication scale per measurement
        public bool MeasurementsIsValue = false;

        public DateTime StartDate { get { return startDate; } }
        public DateTime EndDate { get { return endDate; } }
        public DateTime DefaultStartDate { get { return defaultStartDate; } }
        public DateTime DefaultEndDate { get { return defaultEndDate; } }
        public int Observations { get { return observationLimit; } set { observationLimit = Mathf.Clamp(value, 1, 5000); } }

        public GameObject HexagonSelectionPrefabEmpty;
        public GameObject HexagonSelectionPrefabValue;
      
        protected List<SensorCell> cells = new List<SensorCell>();   
        protected List<SensorCell> staticCells = new List<SensorCell>();        
        protected float edgeMultiplier = 1.15f; //lets add 15% to the edges of the polygon to cover the seems between tiles
        protected int observationLimit = 5000; //the maximum data points per tile retrieved. a low number sometimes causes cells not to properly overlap with other tiles
      

        public struct SensorCell
        {
            public float value;
            public float lat;
            public float lon;
            public SensorPropertyType type;
            public Vector3 unityPosition;
        }

        public virtual void Awake()
        {
            defaultMinValue = Minimum;
            defaultMaxValue = Maximum;
            defaultMinColor = MinColor;
            defaultMaxColor = MaxColor;
            startDate = defaultStartDate;
            endDate = defaultEndDate;
        }

        private SensorPropertyData sensorPropertyData;
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            sensorPropertyData = properties.Get<SensorPropertyData>();

            sensorPropertyData.OnMinValueChanged.AddListener(UpdateMinValue);
            sensorPropertyData.OnMaxValueChanged.AddListener(UpdateMaxValue);
            sensorPropertyData.OnMinColorChanged.AddListener(UpdateMinColor);
            sensorPropertyData.OnMaxColorChanged.AddListener(UpdateMaxColor);
            sensorPropertyData.OnStartDateChanged.AddListener(UpdateStartDate);
            sensorPropertyData.OnEndDateChanged.AddListener(UpdateEndDate);
        }
        
        private void OnDestroy()
        {
            sensorPropertyData.OnMinValueChanged.RemoveListener(UpdateMinValue);
            sensorPropertyData.OnMaxValueChanged.RemoveListener(UpdateMaxValue);
            sensorPropertyData.OnMinColorChanged.RemoveListener(UpdateMinColor);
            sensorPropertyData.OnMaxColorChanged.RemoveListener(UpdateMaxColor);
            sensorPropertyData.OnStartDateChanged.RemoveListener(UpdateStartDate);
            sensorPropertyData.OnEndDateChanged.RemoveListener(UpdateEndDate);
        }

        private void UpdateMinValue(float value)
        {
            Minimum = value;
        }
        
        private void UpdateMaxValue(float value)
        {
            Maximum = value;
        }

        private void UpdateMinColor(Color color)
        {
            MinColor = color;
        }

        private void UpdateMaxColor(Color color)
        {
            MaxColor = color;
        }

        private void UpdateStartDate(DateTime date)
        {
            startDate = date;
        }

        private void UpdateEndDate(DateTime date)
        {
            endDate = date;
        }

        public abstract UnityWebRequest GetRequest(Tile tile, string baseUrl);

        public double[] GetLongLatFromPosition(Vector3 position, Tile tile)
        {
            var unityCoordinate = new Coordinate(
                CoordinateSystem.RD,
                position.x + tile.tileKey.x + 0.5f * tile.layer.tileSize,
                position.z + tile.tileKey.y + 0.5f * tile.layer.tileSize,
                0
            );
            Coordinate coord = unityCoordinate.Convert(CoordinateSystem.WGS84);
            return new double[2] { coord.easting, coord.northing };
        }  
        
        public Vector2Int GetTileKeyFromUnityPosition(Vector3 position, int tileSize)
        {
            var unityCoordinate = new Coordinate(position);
            Coordinate coord = unityCoordinate.Convert(CoordinateSystem.RD);
            Vector2Int key = new Vector2Int(Mathf.RoundToInt(((float)coord.easting - 0.5f * tileSize) / 1000) * 1000, Mathf.RoundToInt(((float)coord.northing - 0.5f * tileSize) / 1000) * 1000);
            return key;
        }

        public Vector2Int GetRDPositionFromUnityPosition(Vector3 position)
        {
            var unityCoordinate = new Coordinate(position);
            Coordinate coord = unityCoordinate.Convert(CoordinateSystem.RD);
            Vector2Int key = new Vector2Int((int)coord.easting, (int)coord.northing);
            return key;
        }
        
        public virtual string GeneratePolygonUrlForTile(Tile tile)
        {
            return string.Empty;
        }

        public double[][] GetLongLatCornersFromTile(Tile tile)
        {
            double[][] coords = new double[4][];
            int tileSize = tile.layer.tileSize;
            coords[0] = GetLongLatFromPosition(new Vector3(-tileSize * 0.5f * edgeMultiplier, 0, tileSize * 0.5f * edgeMultiplier), tile);
            coords[1] = GetLongLatFromPosition(new Vector3(-tileSize * 0.5f * edgeMultiplier, 0, -tileSize * 0.5f * edgeMultiplier), tile);
            coords[2] = GetLongLatFromPosition(new Vector3(tileSize * 0.5f * edgeMultiplier, 0, -tileSize * 0.5f * edgeMultiplier), tile);
            coords[3] = GetLongLatFromPosition(new Vector3(tileSize * 0.5f * edgeMultiplier, 0, tileSize * 0.5f * edgeMultiplier), tile);
            return coords;
        }

        public virtual void ProcessDataFromJson(string json)
        {
            ClearCells();
        }

        public virtual List<SensorCell> GetSensorCellsForTile(Tile tile)
        {
            return Cells;
        }
        
        public void AddCell(SensorCell cell)
        {
            if(StaticSensorData)
                staticCells.Add(cell);
            else
                cells.Add(cell);
        }

        public void ClearCells()
        {
            if (StaticSensorData)            
                staticCells.Clear();
            else
                cells.Clear();
        }

        public Vector3 GetProjectedPositionFromLonLat(double[] coordinate, float height)
        {
            var unityCoordinate = new Coordinate(
                CoordinateSystem.WGS84,
                coordinate[1],
                coordinate[0],
                0
            ).ToUnity();
            unityCoordinate.y = height;
            return unityCoordinate;
        }

    }
}
