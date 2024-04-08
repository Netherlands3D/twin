using System;
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
    }
}