using Netherlands3D.Twin;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.CartesianTiles
{
    public class TileSensorDataController : MonoBehaviour
    {
        public Texture2D DataTexture { get { return dataTexture; } }

        private Texture2D dataTexture;
        private bool hexagonalPatternEnabled = false; //enabling gives a hexagon pattern, disabling only shows colored hexagons with data (disabling increases performance)
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
        private SensorHexagon[,] hexagons = new SensorHexagon[columns, rows];
        private GameObject selectedHexagonObject;
        private float animationSpeed = 30f;
        private Vector2Int lastSelectedHexagonIndex;

        public struct SensorHexagon
        {
            public float value;
            public int measurements;
            public int sensors;
            public Color color;
        }
        
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

        //flat top hexagons, to have a fitting hexagon grid matching a tile grid, we need to offset the height with 1/3 hexagon height so every 3 tiles in the vertical direction will start at the y = 0
        private void UpdateCellOffset(Tile tile)
        {            
            int offsetIndexY = (tile.tileKey.y / tile.layer.tileSize) % 3;
            if (offsetIndexY % 3 != 0)
                tileHexagonOffset.y = (offsetIndexY) * -hexHeight * hexagonSize;
        }

        public void SetCells(Tile tile, SensorDataController controller)
        {
            ClearCells();
            List<SensorDataController.SensorCell> cells = controller.GetSensorCellsForTile(tile);
            localCells = new List<SensorDataController.SensorCell>(cells);
        }    

        public void DeactivateHexagon(Action onDeactivation = null)
        {
            if (selectedHexagonObject != null)
            {                
                //animate the object to scale down
                StartCoroutine(AnimateHexagon(selectedHexagonObject, animationSpeed, 0, instance =>
                {
                    if(instance != null)
                        Destroy(instance);
                    onDeactivation?.Invoke();
                }));                
            }
        }
        
        public void ActivateHexagon(Vector3 position, int tileSize, SensorDataController controller)
        { 
            int innerColumns = columns - 1;
            int rowsInner = rows - 1;
            Vector2 pos = new Vector2(position.x, position.z);
            Vector2 localPosition = new Vector2(
                pos.x - transform.position.x - tileSize * 0.5f,
                pos.y - transform.position.z - tileSize * 0.5f
                );
            Vector2Int index = HexagonPositionToIndex((localPosition.x + tileSize) / tileSize * innerColumns - tileHexagonOffset.x, (localPosition.y + tileSize) / tileSize * rowsInner - tileHexagonOffset.y);
            if (lastSelectedHexagonIndex == index)
            {
                DestroySelectedHexagon();
            }
            DeactivateHexagon();           
            lastSelectedHexagonIndex = index;          

            //lets convert it back from index to be sure of the right position
            Vector2 testPos = HexagonIndexToPosition(index.x, index.y);
            testPos += tileHexagonOffset;
            Vector2 tilePosition = new Vector2(
                testPos.x / innerColumns * tileSize + transform.position.x - tileSize * 0.5f,
                testPos.y / rowsInner * tileSize + transform.position.z - tileSize * 0.5f
                );

            SensorHexagon hex = hexagons[index.x, index.y];
            Color tileColor = hex.color;
            GameObject t;
            if (hex.measurements == 0)
            {
                t = Instantiate(controller.HexagonSelectionPrefabEmpty);
                Material mat = t.GetComponent<MeshRenderer>().material;
                mat.color = new Color(tileColor.r, tileColor.g, tileColor.b, 0.5f);
            }
            else
            {
                t = Instantiate(controller.HexagonSelectionPrefabValue);
                Material mat = t.GetComponent<MeshRenderer>().material;
                mat.color = new Color(tileColor.r, tileColor.g, tileColor.b, 1f);

            }

            //add measuring text to the hexagon object
            GameObject textObject = new GameObject();
            TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();
            textMesh.fontSize = 150;
            RectTransform rt = textMesh.rectTransform;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 75);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 75);
                        
            switch(controller.propertyType)
            {
                case SensorDataController.SensorPropertyType.ThermalDiscomfort:
                    textMesh.text = "metingen: \n" + hex.measurements.ToString() + "\n sensoren: \n" + hex.sensors;
                    break;
                case SensorDataController.SensorPropertyType.RelativeHumidity:
                    textMesh.text = "metingen: \n" + hex.measurements.ToString() + "\n sensoren: \n" + hex.sensors + "\n waarde: \n" + Math.Round(hex.value, 1).ToString() + "%";
                    break;
                case SensorDataController.SensorPropertyType.Temperature:
                    textMesh.text = "metingen: \n" + hex.measurements.ToString() + "\n sensoren: \n" + hex.sensors + "\n waarde: \n" + Math.Round(hex.value, 1).ToString() + "°C";
                    break;

            }            
            textMesh.alignment = TextAlignmentOptions.Center;
            
            textObject.transform.rotation = Quaternion.Euler(new Vector3(90, 0, 30));
            textObject.transform.SetParent(t.transform);
            textObject.transform.localPosition = new Vector3(0,0,0.0051f);

            //previous selected object should already be deactivated so we can use the ref again            
            selectedHexagonObject = t;
            t.transform.rotation = Quaternion.Euler(new Vector3(-90, 0, 30));

            t.transform.position = new Vector3(tilePosition.x, 0, tilePosition.y);
            float scale = hexHeight * 0.5f / innerColumns * tileSize * div2_sqr3 * 2 * 100;
            Vector3 targetScale = new Vector3(scale, scale, 0);
            textObject.transform.localScale = Vector3.one / scale;
            t.transform.localScale = targetScale;

            //animate the object to scale up
            if (gameObject.activeSelf)
            {
                StartCoroutine(AnimateHexagon(t, animationSpeed, scale + hex.measurements * controller.heightMultiplier, instance =>
                {

                }));
            }
        }

        private IEnumerator AnimateHexagon(GameObject instance, float speed, float height, Action<GameObject> onEnd = null)
        {
            Vector3 targetScale = Vector3.zero;
            if(instance != null)
                targetScale = new Vector3(instance.transform.localScale.x, instance.transform.localScale.y, height);
            while(instance != null && Mathf.Abs(instance.transform.localScale.z - height) > 100)
            {
                instance.transform.localScale = Vector3.Slerp(instance.transform.localScale, targetScale, Time.deltaTime * speed);
                yield return new WaitForUpdate();
            }
            if(instance != null)
                instance.transform.localScale = targetScale;
            onEnd?.Invoke(instance);
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

            hexagons = new SensorHexagon[columns, rows];

            for (int row = 0; row < rows; row++)
                for (int col = 0; col < columns; col++)
                {
                    Vector2 position = HexagonIndexToPosition(col, row);
                    position += tileHexagonOffset;

                    Vector2 tilePosition = new Vector2(
                        position.x / columnsInner * tile.layer.tileSize + tile.gameObject.transform.position.x - tile.layer.tileSize * 0.5f,
                        position.y / rowsInner * tile.layer.tileSize + tile.gameObject.transform.position.z - tile.layer.tileSize * 0.5f
                        );

                    //TestHexagon(tile, tilePosition, columnsInner);

                    //get the average cell value from datapoints
                    //because of floating point approximation lets add +0.001f * tilesize 
                    bool hasValues;
                    int measurements, sensors;
                    float value = GetValueForHexagon(tilePosition, hexHeight * 0.5f / columnsInner * tile.layer.tileSize * div2_sqr3 + tile.layer.tileSize * 0.001f, out hasValues, out measurements, out sensors);                    
                    if (!hasValues && !hexagonalPatternEnabled)
                        continue;

                    Color valueColor = Color.gray;
                    if (hasValues)
                    {
                        if (controller.MeasurementsIsValue)
                            value = measurements;

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

                    SensorHexagon hex = new SensorHexagon();
                    hex.value = value;
                    hex.measurements = measurements;
                    hex.sensors = sensors;
                    hex.color = valueColor;
                    hexagons[col, row] = hex;

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
        
        private float GetValueForHexagon(Vector2 hexagonPosition, float hexagonRadius, out bool hasValues, out int measurements, out int sensorCount)
        {
            measurements = 0;
            sensorCount = 0;
            hasValues = false;
            if (localCells == null)
                return 0;

            int cellsInHexagon = 0;
            float value = 0;
            List<Vector2> keys = new List<Vector2>();
            foreach(SensorDataController.SensorCell cell in localCells)
            {
                Vector3 unityPosition = cell.unityPosition;
                if(IsInsideHexagon(hexagonPosition.x, hexagonPosition.y, hexagonRadius * 2, unityPosition.x, unityPosition.z))
                {
                    cellsInHexagon++;
                    value += cell.value;
                    hasValues = true;
                    Vector2 newKey = new Vector2(cell.lon, cell.lat);
                    if(!keys.Contains(newKey))
                        keys.Add(newKey);
                }
            }
            if (cellsInHexagon == 0)
                return 0;

            sensorCount = keys.Count;
            keys.Clear();
            measurements = cellsInHexagon;
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

        public void ClearTexture()
        {
            Color32[] pixels = dataTexture.GetPixels32();
            ClearTexture(pixels);
        }

        private void OnDestroy()
        {
            ClearCells();
            if (dataTexture != null)
                Destroy(dataTexture);
            DestroySelectedHexagon();
        }

        public void DestroySelectedHexagon()
        {
            if (selectedHexagonObject != null)
                Destroy(selectedHexagonObject);
        }

        public void ClearCells()
        {
            if (localCells != null)
                localCells.Clear();
        }

        //private void TestHexagon(Tile tile, Vector2 position, int innerColumns)
        //{
        //    GameObject t = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //    t.GetComponent<MeshRenderer>().material.color = Color.yellow;
        //    t.transform.position = new Vector3(position.x, 50, position.y);
        //    t.transform.localScale = Vector3.one * hexHeight * 0.5f / innerColumns * tile.layer.tileSize * div2_sqr3 * 2;
        //}
    }
}
