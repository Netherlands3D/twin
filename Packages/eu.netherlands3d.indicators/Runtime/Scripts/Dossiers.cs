using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeoJSON.Net.Converters;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Netherlands3D.Coordinates;
using Netherlands3D.Indicators.Data;
using Netherlands3D.SelectionTools;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands.Indicators
{
    public class Dossiers : MonoBehaviour
    {
        [SerializeField] private FreeCamera mainCamera;
        
        [SerializeField] private string dossierUrl = "https://engine.tygron.com/share/provincie-utrecht/mike_test/dossier.geojson";

        public UnityEvent<Dossier> onOpen = new();
        public UnityEvent onFailedToOpen = new();
        public UnityEvent onClose = new();

        public Dossier? ActiveDossier;
        public Variant? ActiveVariant;
        
        private void OnEnable()
        {
            Open(dossierUrl);
        }

        private void OnDisable()
        {
            Close();
        }

        public void Open(string url)
        {
            StartCoroutine(DoLoad(url));
        }

        public void Close()
        {
            onClose.Invoke();
            ActiveDossier = null;
            SelectVariant(null);
        }

        public void SelectVariant(Variant? variant)
        {
            ActiveVariant = variant;
            if (variant.HasValue == false)
            {
                return;
            }

            StartCoroutine(ShowAreaContours(variant.Value));
        }

        private IEnumerator ShowAreaContours(Variant variant)
        {
            var geometryUrl = variant.geometry;
            
            UnityWebRequest www = UnityWebRequest.Get(geometryUrl);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                SelectVariant(null);
                Debug.LogError(www.error);
                yield break;
            }

            var json = www.downloadHandler.text;
            var geometry = JsonConvert.DeserializeObject<FeatureCollection>(
                json, 
                new CrsConverter(), 
                new GeoJsonConverter(), 
                new GeometryConverter(), 
                new LineStringEnumerableConverter(),
                new PointEnumerableConverter(),
                new PolygonEnumerableConverter(),
                new PositionConverter(),
                new PositionEnumerableConverter()
            );
            
            Debug.Log(geometry);
            Debug.Log(geometry.Features.Count);
            var keys = geometry.Features[0].Properties.Keys.ToArray();
            var values = geometry.Features[0].Properties.Values.ToArray();
            for (int i = 0; i < keys.Count(); i++)
            {
                Debug.Log(keys[i]);
                Debug.Log(values[i].GetType().ToString());
                Debug.Log(values[i]);
            }

            PolygonVisualisation polygon = null;
            foreach (var feature in geometry.Features)
            {
                MultiPolygon points = feature.Geometry as MultiPolygon;
                if (points != null)
                {
                    var contours = new List<List<Vector3>>();

                    foreach (Polygon poly in points.Coordinates)
                    {
                        var contour = new List<Vector3>();
                        contours.Add(contour);
                        
                        foreach (LineString line in poly.Coordinates)
                        {
                            var firstPoint = line.Coordinates[0];
                            
                            var firstCoordinate = new Coordinate(
                                CoordinateSystem.WGS84, 
                                firstPoint.Latitude, 
                                firstPoint.Longitude, 
                                firstPoint.Altitude.GetValueOrDefault(0)
                            );

                            Debug.Log(firstCoordinate.ToVector3());
                            var unityCoordinate = CoordinateConverter.ConvertTo(firstCoordinate, CoordinateSystem.Unity).ToVector3();
                            Debug.Log(unityCoordinate);
                                
                            contour.Add(unityCoordinate);
                        }
                        
                        // Close polygon
                        contour.Add(contour[0]);
                    }
                    
                    polygon = PolygonVisualisationUtility.CreateAndReturnPolygonObject(
                        contours,
                        1f,
                        true,
                        true
                    );
                }
            }

            if (polygon != null) mainCamera.FocusOnObject(polygon.gameObject);
        }

        private IEnumerator DoLoad(string url)
        {
            Close();

            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                onFailedToOpen.Invoke();
                Debug.LogError(www.error);
                yield break;
            }

            var json = www.downloadHandler.text;
            Dossier dossier;
            try
            {
                dossier = JsonConvert.DeserializeObject<Dossier>(json);
            }
            catch (Exception e)
            {
                onFailedToOpen.Invoke();
                yield break;
            }

            ActiveDossier = dossier;
            SelectVariant(dossier.variants.FirstOrDefault());

            onOpen.Invoke(dossier);
        }
    }
}