using System;
using System.Collections.Specialized;
using Netherlands3D.Twin.Features;
using Netherlands3D.Web;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Configuration.Indicators
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Configuration/Indicators", fileName = "IndicatorConfiguration", order = 0)]
    public class Configuration : ScriptableObject, IConfiguration
    {
        [SerializeField] private string dossierId;
        
        public UnityEvent<string> OnDossierIdChanged = new();

        public string DossierId
        {
            get => dossierId;
            set
            {
                dossierId = value;
                OnDossierIdChanged.Invoke(value);
            }
        }

        public void Populate(NameValueCollection queryParameters)
        {
            var id = queryParameters.Get("indicators.dossier");
            if (id == null) return;

            Debug.Log($"Set dossier id '{id}' from URL");
            DossierId = id;
        }

        public void AddQueryParameters(UriBuilder urlBuilder)
        {
            urlBuilder.AddQueryParameter("indicators.dossier", dossierId);
        }

        public void Populate(JSONNode jsonNode)
        {
            if (string.IsNullOrEmpty(jsonNode["dossierId"])) return;

            DossierId = jsonNode["dossierId"];
        }

        public JSONNode ToJsonNode()
        {
            return new JSONObject()
            {
                ["dossierId"] = dossierId
            };
        }
    }
}