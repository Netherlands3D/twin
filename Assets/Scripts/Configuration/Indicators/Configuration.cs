using System;
using System.Collections.Specialized;
using Newtonsoft.Json;
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

        public bool Populate(NameValueCollection queryParameters)
        {
            var id = queryParameters.Get("indicators.dossier");
            if (id == null) return false;

            DossierId = id;
            
            return true;
        }

        public string ToQueryString()
        {
            return $"indicators.dossier=${dossierId}";
        }

        public void Populate(JSONNode jsonNode)
        {
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