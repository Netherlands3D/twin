using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Adapters/GeoJSONImportAdapter", fileName = "GeoJSONImportAdapter", order = 0)]
    public class GeoJSONImportAdapter : ScriptableObject
    {
        public void ParseGeoJSON(string file)
        {
            var fullPath = Path.Combine(Application.persistentDataPath, file);
            // var reader = new StreamReader(fullPath);
            CreateGeoJSONLayer(fullPath);

            // JsonTextReader reader = new JsonTextReader(new StringReader(json));
            // GameObject.StartCoroutine(ParseGeoJSON(reader, 1000));
        }

        public static GeoJSONLayer CreateGeoJSONLayer(string filePath)
        {
            var go = new GameObject("GeoJSON");
            var layer = go.AddComponent<GeoJSONLayer>();
            layer.ParseGeoJSON(filePath);
            return layer;
        }
    }
}