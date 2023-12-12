using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace Netherlands3D.Twin
{
    public class TestEsriRasterParsing : MonoBehaviour
    {
        #if UNITY_EDITOR
        [ContextMenu("Load local Esri file")]
        void LoadLocalEsriFile()
        {
            //File select dialog
            string path = UnityEditor.EditorUtility.OpenFilePanel("Select Esri ASCII raster file", "", "asciidxdy");

            if (path.Length != 0)
            {
                var esriRaster = new EsriRasterData();
                esriRaster.ParseASCII(File.ReadAllText(path));

                //We now have the raster data available 
                var rasterData = esriRaster.rasterData;

                //Spawn cubes on raster, and scale Y based on value
                for (int x = 0; x < rasterData.GetLength(0); x++)
                {
                    for (int y = 0; y < rasterData.GetLength(1); y++)
                    {
                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.SetParent(transform);
                        cube.transform.position = new Vector3(x, (float)(rasterData[x, y]), y);
                        cube.transform.localScale = new Vector3(1, (float)(rasterData[x, y]), 1);
                    }
                }
            }
        }
        #endif
    }
}