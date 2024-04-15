using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Web;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Configuration.Indicators
{
    /// <summary>
    /// The configuration class for indicators.
    /// </summary>
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Configuration/Indicators", fileName = "IndicatorConfiguration", order = 0)]
    public class Configuration : ScriptableObject, IConfiguration
    {
        /// <summary>
        /// The base uri for the API endpoint / URI where to find dossiers or to import dossiers.
        ///
        /// This setting can only be set through the configuration file -key:baseUri- and not through query parameters
        /// to prevent possible abuse.
        /// </summary>
        private string baseUri = "https://localhost:7071/api/v1/indicators";
        
        /// <summary>
        /// The identifier for the dossier to retrieve from the api mentioned above.
        ///
        /// This can be set using configuration -key: dossierId- and the query
        /// parameter -parameter: indicators.dossier-.
        /// </summary>
        private string dossierId = "";
        
        /// <summary>
        /// The API key used to validate the request to the backend with.
        /// 
        /// This setting can only be set through the configuration file -key:apiKey- and not through query parameters
        /// to prevent possible abuse.
        /// </summary>
        private string apiKey = "";
        
        public UnityEvent<string> OnDossierIdChanged = new();
        public UnityEvent<string> OnApiKeyChanged = new();
        public UnityEvent<string> OnApiBaseUriChanged = new();

        public string DossierId
        {
            get => dossierId;
            set
            {
                dossierId = value;
                OnDossierIdChanged.Invoke(value);
            }
        }

        public string BaseUri
        {
            get => baseUri;
            set
            {
                baseUri = value;
                OnApiBaseUriChanged.Invoke(value);
            }
        }

        public string ApiKey
        {
            get => apiKey;
            set
            {
                apiKey = value;
                OnApiKeyChanged.Invoke(value);
            }
        }

        public void Populate(NameValueCollection queryParameters)
        {
            var id = queryParameters.Get("indicators.dossier");
            if (string.IsNullOrEmpty(id)) return;

            Debug.Log($"Set dossier id '{id}' from URL");
            DossierId = id;
        }

        public void AddQueryParameters(UriBuilder urlBuilder)
        {
            SetDossierIdInQueryParameters(urlBuilder);
        }

        public List<string> Validate()
        {
            var issues = new List<string>();
            if (string.IsNullOrEmpty(apiKey))
            {
                issues.Add("Een API sleutel is benodigd in de configuratie om kwaliteitsindicatoren te kunnen laden");
            }
            if (string.IsNullOrEmpty(baseUri))
            {
                issues.Add("De Twin is niet geconfigureerd om indicatoren te ondersteunen, de URL naar de backend ontbreekt");
            }
            return issues;
        }

        public void SetDossierIdInQueryParameters(UriBuilder urlBuilder)
        {
            if (string.IsNullOrEmpty(dossierId)) return;

            urlBuilder.AddQueryParameter("indicators.dossier", dossierId);
        }

        public void Populate(JSONNode jsonNode)
        {
            if (string.IsNullOrEmpty(jsonNode["baseUri"]) == false)
            {
                BaseUri = jsonNode["baseUri"];
            }

            if (string.IsNullOrEmpty(jsonNode["apiKey"]) == false)
            {
                ApiKey = jsonNode["apiKey"];
            }

            // The dossier starts loading when setting the DossierId, so make sure this is set last
            if (string.IsNullOrEmpty(jsonNode["dossierId"]) == false)
            {
                DossierId = jsonNode["dossierId"];
            }
        }

        public JSONNode ToJsonNode()
        {
            return new JSONObject()
            {
                ["baseUri"] = baseUri,
                ["dossierId"] = dossierId,
                ["apiKey"] = apiKey
            };
        }
    }
}