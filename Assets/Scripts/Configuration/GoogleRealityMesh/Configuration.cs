using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Netherlands3D.Twin.Functionalities;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Configuration.GoogleRealityMesh
{
    /// <summary>
    /// The configuration class for indicators.
    /// </summary>
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Configuration/GoogleRealityMesh", fileName = "GoogleRealityMeshConfiguration", order = 0)]
    public class Configuration : ScriptableObject, IConfiguration
    {
        /// <summary>
        /// The API key used to query the Google 3D Tiles API.
        /// 
        /// This setting can only be set through the configuration file -key:apiKey- and not through query parameters
        /// to prevent possible abuse.
        /// </summary>
        private string apiKey = "";
        
        public UnityEvent<string> OnApiKeyChanged = new();

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
        }

        public void AddQueryParameters(UriBuilder urlBuilder)
        {
        }

        public void Populate(JSONNode jsonNode)
        {
            if (string.IsNullOrEmpty(jsonNode["apiKey"]) == false)
            {
                ApiKey = jsonNode["apiKey"];
            }
        }

        public JSONNode ToJsonNode()
        {
            return new JSONObject()
            {
                ["apiKey"] = apiKey
            };
        }

        /// <summary>
        /// Returns a list of error messages when the configuration is not valid, or no messages if
        /// the configuration _is_ valid.
        /// </summary>
        public List<string> Validate()
        {
            var errorMessages = new List<string>();

            if (string.IsNullOrEmpty(apiKey))
            {
                errorMessages.Add(
                "Google API sleutel ontbreekt, vraag de applicatiebeheerder om deze in te stellen."
                );
            }

            return errorMessages;
        }
    }
}