using Netherlands3D.Twin;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.CartesianTiles
{
    public class TileSensorDataController : MonoBehaviour
    {
        public Texture2D DataTexture { get { return dataTexture; } }

        private Texture2D dataTexture;
        private bool hexagonalPatternEnabled = true; //enabling gives a hexagon pattern, disabling only shows colored hexagons with data (disabling increases performance)
        private const float sqr3 = 1.73205080757f;
        private const float sqr3_div4 = 0.25f * sqr3;
        private const float half_sqr3_div4 = 0.5f * sqr3_div4;
        private const float div2_sqr3 = 2f / sqr3; 
        private const int textureWidth = 512;
        private const int textureHeight = 512;
        private const float hexagonSize = 2f / 3;
        private const float hexWidth = 1.5f * hexagonSize;
        private const float hexHeight = sqr3 * hexagonSize;
        private const int columns = 11;
        private const int rows = 11;
        private float hexagonEdgeWidth = 0.03f;
        private Vector2 tileHexagonOffset = Vector2.zero;
        private List<SensorDataController.SensorCell> localCells;
        
        public void Initialize()
        {
            if (!hexagonalPatternEnabled)
                hexagonEdgeWidth = 0;

            if (dataTexture == null || dataTexture.width != textureWidth || dataTexture.height != textureHeight)
            {
                dataTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAHalf, false);
                dataTexture.filterMode = FilterMode.Bilinear;
                dataTexture.wrapMode = TextureWrapMode.Clamp;
            }
        }        

        private void UpdateCellOffset(Tile tile)
        {
            //flat top hexagons
            //to have a fitting hexagon grid matching a tile grid, we need to offset the height with 1/3 hexagon height so every 3 tiles in the vertical direction will start at the y = 0
            //tile 0,0 hex 0,0 at 0,0
            //tile 0,1 hex 0,0 at 0, -hexheight * 1/3
            //tile 0,2 hex 0,0 at 0, -hexheight * 2/3
            //tile 0,3 hex 0,0 at 0,0
            int offsetIndexY = (tile.tileKey.y / tile.layer.tileSize) % 3;
            if (offsetIndexY % 3 != 0)
                tileHexagonOffset.y = (offsetIndexY) * -hexHeight * hexagonSize;

            //TODO offset for different scales 
            //int offsetIndexX = (tile.tileKey.x / tile.layer.tileSize) % 3;
            //if(offsetIndexX % 3 != 0)
            //    tileHexagonOffset.x = (offsetIndexX) * -hexWidth * ;
        }

        public void SetCells(Tile tile, SensorDataController controller)
        {
            ClearCells();
            List<SensorDataController.SensorCell> cells = controller.GetSensorCellsForTile(tile);
            localCells = new List<SensorDataController.SensorCell>(cells);
        }       

        public void UpdateTexture(Tile tile, SensorDataController controller)
        {         
            UpdateCellOffset(tile);

            int columnsInner = columns - 1;
            int rowsInner = rows - 1;  
            float hexInnerRadius = hexWidth * 0.5f;
            float texInnerHexRadius = hexInnerRadius / columnsInner * textureWidth * div2_sqr3 * (1f - hexagonEdgeWidth);
            float hexOuterRadius = hexHeight * 0.5f;
            float texOuterHexRadius = hexOuterRadius / columnsInner * textureWidth * div2_sqr3 * (1f - hexagonEdgeWidth);           
            Color32[] pixels = dataTexture.GetPixels32();

            ClearTexture(pixels);

            for (int row = 0; row < rows; row++)
                for (int col = 0; col < columns; col++)
                {
                    Vector2 position = HexagonIndexToPosition(col, row);
                    position += tileHexagonOffset;

                    Vector2 tilePosition = new Vector2(
                        position.x / columnsInner * tile.layer.tileSize + tile.gameObject.transform.position.x - tile.layer.tileSize * 0.5f,
                        position.y / rowsInner * tile.layer.tileSize + tile.gameObject.transform.position.z - tile.layer.tileSize * 0.5f
                        );
                    
                    //get the average cell value from datapoints
                    //because of floating point approximation lets add +0.001f * tilesize 
                    bool hasValues;                    
                    float value = GetValueForHexagon(tilePosition, hexHeight * 0.5f / columnsInner * tile.layer.tileSize * div2_sqr3 + tile.layer.tileSize * 0.001f, out hasValues);                    
                    if (!hasValues && !hexagonalPatternEnabled)
                        continue;

                    Color valueColor = Color.gray;
                    if (hasValues)
                    {
                        float total = controller.Maximum - controller.Minimum; 
                        float v = (value - controller.Minimum) / total; 
                        v = Mathf.Clamp01(v);
                        valueColor = Color.Lerp(controller.MinColor, controller.MaxColor, v);
                        valueColor.a = 1f;
                    }
                    else
                    {
                        valueColor.a = 0.0f;
                    }

                    //generate the hexagon pixels
                    Vector2 texPosition = new Vector2(position.x / columnsInner * textureWidth, position.y / rowsInner * textureHeight);
                    for (int x = (int)(texPosition.x - texOuterHexRadius); x < (int)texPosition.x + texOuterHexRadius; x++)
                    {
                        for (int y = (int)(texPosition.y - texOuterHexRadius); y < (int)texPosition.y + texOuterHexRadius; y++)
                        {
                            float distX = Mathf.Abs(texPosition.x - x);
                            float distY = Mathf.Abs(texPosition.y - y);
                            float distSqrd = distX * distX + distY * distY;
                            if (distSqrd < texOuterHexRadius * texOuterHexRadius)
                            {
                                if (distSqrd >= texInnerHexRadius * texInnerHexRadius) 
                                {
                                    if(IsInsideHexagon(texPosition.x, texPosition.y, texOuterHexRadius * 2, x, y))
                                    {
                                        if (x >= 0 && y >= 0 && x < textureWidth && y < textureHeight)
                                        {
                                            pixels[x + y * textureWidth] = valueColor;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x >= 0 && y >= 0 && x < textureWidth && y < textureHeight)
                                    {
                                        pixels[x + y * textureWidth] = valueColor;
                                    }
                                }
                            }
                        }
                    }
                }

            dataTexture.SetPixels32(pixels);
            dataTexture.Apply();
        }
        
        private float GetValueForHexagon(Vector2 hexagonPosition, float hexagonRadius, out bool hasValues)
        {
            hasValues = false;
            if (localCells == null)
                return 0;

            int cellsInHexagon = 0;
            float value = 0;
            foreach(SensorDataController.SensorCell cell in localCells)
            {
                Vector3 unityPosition = cell.unityPosition;
                if(IsInsideHexagon(hexagonPosition.x, hexagonPosition.y, hexagonRadius * 2, unityPosition.x, unityPosition.z))
                {
                    cellsInHexagon++;
                    value += cell.value;
                    hasValues = true;
                }
            }
            if (cellsInHexagon == 0)
                return 0;

            value /= cellsInHexagon;
            return value;
        }

        private Vector2 HexagonIndexToPosition(int col, int row)
        {
            float x = col * hexWidth;
            float y = row * hexHeight + (col % 2) * hexHeight * 0.5f;
            return new Vector2(x, y);
        }

        private Vector2Int HexagonPositionToIndex(float x, float y)
        {
            int col = (int)(x / hexWidth + 0.5f * hexWidth);
            int row = (int)(y / hexHeight - ((col % 2) * hexHeight * 0.5f) + 0.5f * hexHeight);
            return new Vector2Int(col, row);
        }
      
        public bool IsInsideHexagon(float x0, float y0, float d, float x, float y)
        {
            float dx = Mathf.Abs(x - x0) / d;
            float dy = Mathf.Abs(y - y0) / d;            
            return (dy <= sqr3_div4) && (sqr3_div4 * dx + 0.25f * dy <= half_sqr3_div4);
        }        

        private void ClearTexture(Color32[] pixels)
        {
            Color clearColor;
            if (hexagonalPatternEnabled)
                clearColor = Color.white;
            else
                clearColor = Color.clear;
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    pixels[x + y * textureWidth] = clearColor;
                }
            }
        }

        private void OnDestroy()
        {
            ClearCells();
            if (dataTexture != null)
                Destroy(dataTexture);
        }

        public void ClearCells()
        {
            if (localCells != null)
                localCells.Clear();
        }

        //DEBUG//

        //private List<GameObject> sensorTestPositions = new List<GameObject>();
        //public void ProjectAllSensorPositions()
        //{
        //    if (sensorTestPositions.Count > 0)
        //    {
        //        for (int i = sensorTestPositions.Count - 1; i >= 0; i--)
        //            Destroy(sensorTestPositions[i]);
        //    }

        //    sensorTestPositions.Clear();
        //    List<double[]> positions = GetAllLonLatPositions();
        //    foreach (double[] position in positions)
        //    {
        //        Vector3 unityPosition = SensorDataController.GetProjectedPositionFromLonLat(position, ImageProjectionLayer.ProjectorHeight);
        //        GameObject test = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //        test.transform.position = unityPosition + Vector3.up * (float)position[2];
        //        test.transform.localScale = Vector3.one * 50;
        //        sensorTestPositions.Add(test);
        //    }
        //}

        //public List<double[]> GetAllLonLatPositions()
        //{
        //    List<double[]> result = new List<double[]>();
        //    if (localCells == null)
        //        return result;

        //    foreach (var cell in localCells)
        //        result.Add(new double[3] { cell.lon, cell.lat, (int)cell.type * 50 });
        //    return result;
        //}

        //use this to test positions and radius for hexagons
        //TestHexagon(tile, tilePosition, columnsInner); in updatetexture within the col and row loops
        //private void TestHexagon(Tile tile, Vector2 position, int innerColumns)
        //{
        //    GameObject t = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //    t.GetComponent<MeshRenderer>().material.color = Color.yellow;
        //    t.transform.position = new Vector3(position.x, 50, position.y);
        //    t.transform.localScale = Vector3.one * hexHeight * 0.5f / innerColumns * tile.layer.tileSize * div2_sqr3 * 2;
        //}

        //public void SetCornerCoordsTest(double[][] cornerCoords)
        //{
        //    this.cornerCoords = cornerCoords;
        //}

        //private void ClearSensorTestPositions()
        //{
        //    if (sensorTestPositions.Count == 0)
        //        return;

        //    for (int i = sensorTestPositions.Count - 1; i >= 0; i--)
        //        Destroy(sensorTestPositions[i]);

        //    sensorTestPositions.Clear();
        //}

        //Vector3[] corners = new Vector3[4];
        //double[][] cornerCoords = new double[4][];
        //private void OnDrawGizmos()
        //{
        //    for(int i = 0; i < 4; i++) 
        //    {
        //        corners[i] = SensorDataController.GetProjectedPositionFromLonLat(cornerCoords[i], SensorProjectionLayer.ProjectorHeight);
        //    }

        //    Debug.DrawLine(corners[0], corners[1], Color.red);
        //    Debug.DrawLine(corners[1], corners[2], Color.red);
        //    Debug.DrawLine(corners[2], corners[3], Color.red);
        //    Debug.DrawLine(corners[3], corners[0], Color.red);
        //    Debug.DrawLine(corners[0], corners[2], Color.red);
        //    Debug.DrawLine(corners[1], corners[3], Color.red);

        //    if (localCells == null)
        //        return;

        //    double[] lonlat = new double[2];
        //    foreach (SensorDataController.UrbanReleafCell cell in localCells)
        //    {
        //        lonlat[0] = cell.lon;
        //        lonlat[1] = cell.lat;
        //        Vector3 unityPosition = SensorDataController.GetProjectedPositionFromLonLat(lonlat, 0);
        //        Debug.DrawLine(gameObject.transform.position, unityPosition, Color.cyan);
        //    }
        //}
    }
}
