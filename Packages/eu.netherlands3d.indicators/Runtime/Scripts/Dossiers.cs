using System;
using System.Collections;
using System.Linq;
using GeoJSON.Net.Converters;
using GeoJSON.Net.Feature;
using Netherlands3D.Indicators.Data;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands.Indicators
{
    public class Dossiers : MonoBehaviour
    {
        [SerializeField] private string dossierUrl = "https://engine.tygron.com/share/provincie-utrecht/mike_test/dossier.geojson";

        public UnityEvent<Dossier> onOpen = new();
        public UnityEvent onFailedToOpen = new();
        public UnityEvent<FeatureCollection> onLoadedProjectArea = new();
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
            onLoadedProjectArea.Invoke(null);
            onClose.Invoke();
            ActiveDossier = null;
            SelectVariant(null);
        }

        public void SelectVariant(Variant? variant)
        {
            ActiveVariant = variant;
            if (variant.HasValue == false)
            {
                onLoadedProjectArea.Invoke(null);
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

            onLoadedProjectArea.Invoke(
                JsonConvert.DeserializeObject<FeatureCollection>(
                    www.downloadHandler.text, 
                    new CrsConverter(), 
                    new GeoJsonConverter(), 
                    new GeometryConverter(), 
                    new LineStringEnumerableConverter(),
                    new PointEnumerableConverter(),
                    new PolygonEnumerableConverter(),
                    new PositionConverter(),
                    new PositionEnumerableConverter()
                )
            );
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