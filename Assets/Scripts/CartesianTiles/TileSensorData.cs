using Netherlands3D.Twin;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.CartesianTiles
{
    public class TileSensorData : MonoBehaviour
    {
        public Texture2D DataTexture { get { return dataTexture; } }

        [SerializeField]
        private Texture2D dataTexture;

        private const float sqr3 = 1.73205080757f;
        private const int textureWidth = 512;
        private const int textureHeight = 512;
        private const float hexagonSize = 2f / 3;
        private const float hexWidth = 1.5f * hexagonSize;
        private const float hexHeight = sqr3 * hexagonSize;
        private const int columns = 11;
        private const int rows = 11;
        private Vector2 tileHexagonOffset = Vector2.zero;

        //notes (flat top hexagons)
        //to have a fitting hexagon grid matching a tile grid, we need to offset the height with 1/3 hexagon height so every 3 tiles in the vertical direction will start at the y = 0
        //tile 0,0 hex 0,0 at 0,0
        //tile 0,1 hex 0,0 at 0, -hexheight * 1/3
        //tile 0,2 hex 0,0 at 0, -hexheight * 2/3
        //tile 0,3 hex 0,0 at 0,0

        public void CreateTexture()
        {
            if (dataTexture == null || dataTexture.width != textureWidth || dataTexture.height != textureHeight)
            {
                dataTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false);
                dataTexture.filterMode = FilterMode.Bilinear;
                dataTexture.wrapMode = TextureWrapMode.Clamp;
            }
        }

        public void UpdateTexture(Tile tile, SensorDataController controller)
        {
            int offsetIndexY = (tile.tileKey.y / tile.layer.tileSize) % 3;
            if(offsetIndexY % 3 != 0)
                tileHexagonOffset.y = (offsetIndexY) * -hexHeight * hexagonSize;

            //TODO offset for different scales 
            //int offsetIndexX = (tile.tileKey.x / tile.layer.tileSize) % 3;
            //if(offsetIndexX % 3 != 0)
            //    tileHexagonOffset.x = (offsetIndexX) * -hexWidth * ;

            float hexagonEdgeWidth = 0.05f;
            float hexInnerRadius = hexWidth * 0.5f;
            float texInnerHexRadius = hexInnerRadius / (columns - 1) * textureWidth * 2 / sqr3 * (1f - hexagonEdgeWidth);
            float hexOuterRadius = hexHeight * 0.5f;
            float texOuterHexRadius = hexOuterRadius / (columns - 1) * textureWidth * 2 / sqr3 * (1f - hexagonEdgeWidth);

            Color clearColor = Color.gray;
            Color32[] pixels = dataTexture.GetPixels32();
            for (int x = 0; x < textureWidth; x++)
            {
                for (int y = 0; y < textureHeight; y++)
                {
                    pixels[x + y * textureWidth] = clearColor;
                }
            }

            for (int row = 0; row < rows; row++)
                for (int col = 0; col < columns; col++)
                {
                    Vector2 position = HexagonIndexToPosition(col, row);
                    position += tileHexagonOffset;

                    Vector2 tilePosition = new Vector2(
                        position.x / (columns - 1) * tile.layer.tileSize + tile.gameObject.transform.position.x - tile.layer.tileSize * 0.5f,
                        position.y / (rows - 1) * tile.layer.tileSize + tile.gameObject.transform.position.z - tile.layer.tileSize * 0.5f
                        );

                    //use this to test positions and radius for hexagons
                    //GameObject t = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    //t.GetComponent<MeshRenderer>().material.color = Color.yellow;
                    //t.transform.position = new Vector3(tilePosition.x, 50, tilePosition.y);
                    //t.transform.localScale = Vector3.one * hexHeight * 0.5f / (columns - 1) * tile.layer.tileSize * 2;


                    float value = GetValueForHexagon(tilePosition, hexHeight * 0.5f / (columns - 1) * tile.layer.tileSize * 2 / sqr3, controller);
                    if (value > 0)
                    {

                    }


                    Vector2 texPosition = new Vector2(position.x / (columns - 1) * textureWidth, position.y / (rows - 1) * textureHeight);
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
                                            pixels[x + y * textureWidth] = new Color(value, 0, 0, 1);
                                        }
                                    }
                                }
                                else
                                {
                                    if (x >= 0 && y >= 0 && x < textureWidth && y < textureHeight)
                                    {
                                        pixels[x + y * textureWidth] = new Color(value, 0, 0, 1);
                                    }
                                }
                            }
                        }
                    }
                }



            //TODO: CREATE TEXTURE FROM DATA
            //form hex grid based on LOD
            //draw pixel set based on precalculated math
            //setpixels32
            //texture apply
            //interval 30 sec update
            //callbacks?

            dataTexture.SetPixels32(pixels);
            dataTexture.Apply();
        }

        //TODO fix edge cases for neighbouring tiles
        private float GetValueForHexagon(Vector2 hexagonPosition, float hexagonRadius, SensorDataController controller)
        {
            int cellsInHexagon = 0;
            float value = 0;
            double[] lonlat = new double[2];
            List<SensorDataController.UrbanReleafCell> cells = controller.Cells;
            foreach(SensorDataController.UrbanReleafCell cell in cells)
            {
                lonlat[0] = cell.lon;
                lonlat[1] = cell.lat;
                Vector3 unityPosition = controller.GetProjectedPositionFromLonLat(lonlat, 0);
                if(IsInsideHexagon(hexagonPosition.x, hexagonPosition.y, hexagonRadius * 2, unityPosition.x, unityPosition.z))
                {
                    cellsInHexagon++;
                    value += cell.value;
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

        private const float sqr3_div4 = 0.25f * sqr3;
        private const float half_sqr3_div4 = 0.5f * sqr3_div4;
        public bool IsInsideHexagon(float x0, float y0, float d, float x, float y)
        {
            float dx = Mathf.Abs(x - x0) / d;
            float dy = Mathf.Abs(y - y0) / d;            
            return (dy <= sqr3_div4) && (sqr3_div4 * dx + 0.25f * dy <= half_sqr3_div4);
        }
    }
}
