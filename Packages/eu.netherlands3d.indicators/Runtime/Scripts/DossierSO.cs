using System;
using System.Collections;
using System.Linq;
using Netherlands3D.Indicators.Dossiers;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Indicators
{
    [CreateAssetMenu(menuName = "Netherlands3D/Dossier", fileName = "DossierSO", order = 0)]
    public class DossierSO : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Contains the URI Template where to find the dossier's JSON file. The dossier id can be inserted using {id} (without spaces).")]
        private string dossierUriTemplate = "";

        public UnityEvent<Dossier> onOpen = new();
        public UnityEvent<Variant?> onSelectedVariant = new();
        public UnityEvent<ProjectArea?> onSelectedProjectArea = new();
        public UnityEvent onFailedToOpen = new();
        public UnityEvent onClose = new();
        public UnityEvent<FeatureCollection> onLoadedProjectArea = new();

        public Dossier? Data { get; private set; }
        public Variant? ActiveVariant { get; private set; }

        private ProjectArea? activeProjectArea;
        public ProjectArea? ActiveProjectArea
        {
            get => activeProjectArea;
            set
            {
                onSelectedProjectArea.Invoke(value);
                activeProjectArea = value;
            }
        }

        public IEnumerator Open(string dossierId)
        {
            string url = AssembleUri(dossierId);
            Debug.Log($"<color=orange>Loading dossier with id {dossierId} from {url}</color>");
            Close();

            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                onFailedToOpen.Invoke();
                Debug.Log($"<color=red>Failed to load dossier from {url}</color>");
                Debug.LogError(www.error);
                yield break;
            }

            var json = www.downloadHandler.text;
            Debug.Log($"<color=orange>Received dossier data from {url}</color>");
            Debug.Log(json);

            Dossier dossier;
            try
            {
                dossier = JsonConvert.DeserializeObject<Dossier>(json);
            }
            catch (Exception e)
            {
                Debug.Log($"<color=red>Failed to deserialize dossier from {url}</color>");
                Debug.LogError(e.Message);
                onFailedToOpen.Invoke();
                yield break;
            }

            Debug.Log($"<color=green>Loaded dossier with id {dossier.id} from {url}</color>");

            Data = dossier;
            onOpen.Invoke(dossier);

            SelectVariant(dossier.variants.FirstOrDefault());
        }

        public void Close()
        {
            onClose.Invoke();
            Data = null;
            SelectVariant(null);
            ActiveProjectArea = null;
        }

        public void SelectVariant(Variant? variant)
        {
            ActiveVariant = variant;
            onSelectedVariant.Invoke(variant);

            if (variant.HasValue != false) return;
        }

        public IEnumerator LoadProjectAreaGeometry(Variant variant)
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

            var featureCollection = JsonConvert.DeserializeObject<FeatureCollection>(www.downloadHandler.text);
            onLoadedProjectArea.Invoke(featureCollection);
        }

        private string AssembleUri(string dossierId)
        {
            return dossierUriTemplate.Replace("{id}", dossierId);
        }
    }
}