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

    const string SrcDir = "/Users/Tom/Documents/TSCD/Repos/NL3DPackages/Twin/Assets/StreamingAssets/ATMBuildingGeojson/";
    const string DstDir = "/Users/Tom/Documents/TSCD/Repos/NL3DPackages/Twin/Assets/StreamingAssets/ATMBuildingGeojson/Tiles/";

    static double MinX = double.MaxValue;
    static double MaxX = double.MinValue;
    static double MinY = double.MaxValue;
    static double MaxY = double.MinValue;

    static readonly Dictionary<Vector2Int, FeatureCollection> Tiles = new Dictionary<Vector2Int, FeatureCollection>();

    public void Start()
    {
        var SrcFile = Path.Combine(SrcDir, $"{baseName}{extension}" );
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
            string filePath = Path.Combine(DstDir, $"{baseName}{key}{extension}");
            File.WriteAllText(filePath, kvp.Value.ToJson());
        }

        Debug.Log("Done.");
    }

    // static TileBBox GetTileBBox(JToken polygon)
    // {
    //     double minLon = double.PositiveInfinity, maxLon = double.NegativeInfinity;
    //     double minLat = double.PositiveInfinity, maxLat = double.NegativeInfinity;
    //
    //     foreach (var point in polygon)
    //     {
    //         double lon = (double)point[0];
    //         double lat = (double)point[1];
    //
    //         minLon = Math.Min(minLon, lon);
    //         maxLon = Math.Max(maxLon, lon);
    //         minLat = Math.Min(minLat, lat);
    //         maxLat = Math.Max(maxLat, lat);
    //     }
    //
    //     var minXY = ProjectToRDMeters(minLat, minLon);
    //     var maxXY = ProjectToRDMeters(maxLat, maxLon);
    //
    //     return new TileBBox
    //     {
    //         MinX = (int)(minXY.X / TileSize),
    //         MinY = (int)(minXY.Y / TileSize),
    //         MaxX = (int)(maxXY.X / TileSize),
    //         MaxY = (int)(maxXY.Y / TileSize)
    //     };
    // }

    // static void AddToTiles(JToken feature, TileBBox tileBBox)
    // {
    //     for (int y = tileBBox.MinY; y <= tileBBox.MaxY; y++)
    //     {
    //         for (int x = tileBBox.MinX; x <= tileBBox.MaxX; x++)
    //         {
    //             Vector2Int key = new Vector2Int(x, y);
    //             if (!Tiles.ContainsKey(key))
    //             {
    //                 MaxX = Math.Max(MaxX, x);
    //                 MaxY = Math.Max(MaxY, y);
    //                 MinX = Math.Min(MinX, x);
    //                 MinY = Math.Min(MinY, y);
    //
    //                 Tiles[key] = new FeatureCollection();
    //             }
    //
    //             Tiles[key].Features.Add(feature);
    //         }
    //     }
    // }

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
        // return new Point { X = rd.x, Y = rd.y };
    }

    class FeatureCollection
    {
        public List<JToken> Features { get; } = new List<JToken>();

        public string ToJson() => JObject.FromObject(new { type = "FeatureCollection", features = Features }).ToString();
    }

    // class TileBBox
    // {
    //     public int MinX { get; set; }
    //     public int MinY { get; set; }
    //     public int MaxX { get; set; }
    //     public int MaxY { get; set; }
    // }

    // struct Point
    // {
    //     public double X;
    //     public double Y;
    // }
}