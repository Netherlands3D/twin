using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Coordinates;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class GeoJsonTiler : MonoBehaviour
{
    [SerializeField] private string baseName = "1802";
    [SerializeField] private string extension = ".geojson";
    const int TileSize = 1000;

    const string SrcDir = "ATMBuildingGeojson/";
    const string DstDir = "ATMBuildingGeojson/Tiles/";

    static double MinX = double.MaxValue;
    static double MaxX = double.MinValue;
    static double MinY = double.MaxValue;
    static double MaxY = double.MinValue;

    static readonly Dictionary<Vector2Int, FeatureCollection> Tiles = new Dictionary<Vector2Int, FeatureCollection>();

    public void Start()
    {
        var SrcFile = Path.Combine(Application.streamingAssetsPath, SrcDir, $"{baseName}{extension}");
        string geojsonString = File.ReadAllText(SrcFile);
        JObject geojson = JObject.Parse(geojsonString);
        var features = geojson["features"];

        Debug.Log($"Processing {features.Count()} features...");

        foreach (var feature in features)
        {
            var coordinates = feature["geometry"]["coordinates"];
            double lon = (double)coordinates[0];
            double lat = (double)coordinates[1];

            var c = ProjectToRDMeters(lat, lon);
            var xKey = Mathf.FloorToInt((float)(c.x / TileSize)) * 1000;
            var yKey = Mathf.FloorToInt((float)(c.y / TileSize)) * 1000;
            var tileKey = new Vector2Int(xKey, yKey);
            AddToTiles(feature, tileKey);
        }

        Debug.Log("processed into " + Tiles.Count + " tiles");

        foreach (var kvp in Tiles)
        {
            Vector2Int key = kvp.Key;
            Debug.Log($"Writing tile {key}.json, {kvp.Value.Features.Count} features");
            string filePath = Path.Combine(Application.streamingAssetsPath, DstDir, baseName, $"{baseName}{key}{extension}");
            File.WriteAllText(filePath, kvp.Value.ToJson());
        }

        Debug.Log("Done.");
    }

    static void AddToTiles(JToken feature, Vector2Int key)
    {
        if (!Tiles.ContainsKey(key))
        {
            Tiles[key] = new FeatureCollection();
        }

        Tiles[key].Features.Add(feature);
    }

    static Vector3RD ProjectToRDMeters(double latitude, double longitude)
    {
        Coordinate c = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, latitude, longitude, 0);
        var rdCoord = c.Convert(CoordinateSystem.RDNAP);
        var rd = rdCoord.ToVector3RD();

        return rd;
    }

    class FeatureCollection
    {
        public List<JToken> Features { get; } = new List<JToken>();

        public string ToJson() => JObject.FromObject(new { type = "FeatureCollection", features = Features }).ToString();
    }
}