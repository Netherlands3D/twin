using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Twin
{
    public abstract class SensorDataController : MonoBehaviour
    {
        public bool StaticSensorData = false;
        public enum SensorPropertyType { None, Temperature, RelativeHumidity, ThermalDiscomfort }
        public SensorPropertyType propertyType;
        public List<SensorCell> Cells { get { return StaticSensorData ? staticCells : cells; } }
        public float Maximum;
        public float Minimum;        
        public Color MaxColor;
        public Color MinColor;  
        public int StartTimeSeconds { get { return startTimeWindowSeconds; } set { startTimeWindowSeconds = Mathf.Clamp(value, 0, DaySeconds * MaxDays); } }
        public int EndTimeSeconds { get { return endTimeWindowSeconds; } set { endTimeWindowSeconds = Mathf.Clamp(value, 0, DaySeconds * MaxDays); } }
        public DateTime DefaultStartDate { get { return defaultStartDate; } }
        public DateTime DefaultEndDate { get { return defaultEndDate; } }
        public int Observations { get { return observationLimit; } set { observationLimit = Mathf.Clamp(value, 1, 5000); } }

        public GameObject HexagonSelectionPrefabEmpty;
        public GameObject HexagonSelectionPrefabValue;


        public const int DaySeconds = 3600 * 24;
        public const int MaxDays = 365;
        protected List<SensorCell> cells = new List<SensorCell>();   
        protected List<SensorCell> staticCells = new List<SensorCell>();        
        protected float edgeMultiplier = 1.15f; //lets add 15% to the edges of the polygon to cover the seems between tiles
        protected int observationLimit = 5000; //the maximum data points per tile retrieved. a low number sometimes causes cells not to properly overlap with other tiles
        protected int startTimeWindowSeconds = MaxDays * DaySeconds;
        protected int endTimeWindowSeconds = DaySeconds;
        protected DateTime defaultStartDate = new();
        protected DateTime defaultEndDate = new();

        public struct SensorCell
        {
            public float value;
            public float lat;
            public float lon;
            public SensorPropertyType type;
            public Vector3 unityPosition;
        }

        public virtual void Start()
        {

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
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.WGS84);
            return new double[2] { coord.Points[1], coord.Points[0] };
        }  
        
        public Vector2Int GetTileKeyFromUnityPosition(Vector3 position, int tileSize)
        {
            var unityCoordinate = new Coordinate(
                CoordinateSystem.Unity,
                position.x,
                position.y,
                position.z
            );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.RD);
            Vector2Int key = new Vector2Int(Mathf.RoundToInt(((float)coord.Points[0] - 0.5f * tileSize) / 1000) * 1000, Mathf.RoundToInt(((float)coord.Points[1] - 0.5f * tileSize) / 1000) * 1000);
            return key;
        }

        public Vector2Int GetRDPositionFromUnityPosition(Vector3 position)
        {
            var unityCoordinate = new Coordinate(
                CoordinateSystem.Unity,
                position.x,
                position.y,
                position.z
            );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.RD);
            Vector2Int key = new Vector2Int((int)coord.Points[0], (int)coord.Points[1]);
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
            );
            Coordinate coord = CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.Unity);
            Vector3 position = coord.ToUnity();
            position.y = height;
            return position;
        }
    }
}
