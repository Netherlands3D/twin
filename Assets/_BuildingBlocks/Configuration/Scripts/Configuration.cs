using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Web;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.Configuration
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Configuration", fileName = "Configuration", order = 0)]
    public class Configuration : ScriptableObject, IConfiguration
    {
        [SerializeField] private string title = "Amersfoort";
        [SerializeField] private string projectUrl = Path.Combine(Application.streamingAssetsPath, "ProjectTemplate.nl3d");
        [SerializeField] private Coordinate origin = new(CoordinateSystem.RDNAP, 155207, 462945, 0);
        [SerializeField] public List<Functionality> Functionalities = new();
        [SerializeField] private string corsProxyUrl = null;

        public string Title
        {
            get => title;
            set
            {
                title = value;
                OnTitleChanged.Invoke(value);
            }
        }

        public string CorsProxyUrl => corsProxyUrl;
        public string ProjectUrl => projectUrl;

        public Coordinate Origin
        {
            get => origin;
            set
            {
                var roundedValue = new Coordinate(value.CoordinateSystem, (int)value.value1, (int)value.value2, (int)value.value3);
                origin = roundedValue;
                OnOriginChanged.Invoke(roundedValue);
            }
        }

        /// <summary>
        /// By default, the options to change settings are enabled for the user.
        /// The configuration file can disable this.
        /// </summary>
        private bool allowUserSettings = true;

        public bool AllowUserSettings
        {
            get => allowUserSettings;
            set
            {
                allowUserSettings = value;
                OnAllowUserSettingsChanged.Invoke(allowUserSettings);
            }
        }

        /// <summary>
        /// By default, we should start the setup wizard to configure the twin, unless configuration was successfully
        /// loaded from the URL or from the Configuration File.
        /// </summary>
        private bool shouldStartSetup = true;

        public bool ShouldStartSetup
        {
            get => shouldStartSetup;
            set
            {
                shouldStartSetup = value;
                OnShouldStartSetupChanged.Invoke(shouldStartSetup);
            }
        }

        public UnityEvent<bool> OnAllowUserSettingsChanged = new();
        public UnityEvent<bool> OnShouldStartSetupChanged = new();
        public UnityEvent<Coordinate> OnOriginChanged = new();
        public UnityEvent<string> OnTitleChanged = new();

        /// <summary>
        /// Overwrites the contents of this Scriptable Object with the serialized JSON file at the provided location.
        /// </summary>
        public IEnumerator PopulateFromFile(string externalConfigFilePath)
        {
            Debug.Log($"Attempting to load configuration from {externalConfigFilePath}");
            using UnityWebRequest request = UnityWebRequest.Get(externalConfigFilePath);
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Successfully downloaded external config: {externalConfigFilePath}");
                var json = request.downloadHandler.text;

                // populate object and when settings are missing, use the defaults from the provided object
                Populate(JSON.Parse(json));
                ShouldStartSetup = false;
            }
            else
            {
                Debug.LogWarning($"Could not load: {externalConfigFilePath}. Using default config.");
            }

            yield return null;
        }

        public void Populate(Uri url)
        {
            var queryParameters = new NameValueCollection();
            url.TryParseQueryString(queryParameters);
            Populate(queryParameters);
        }

        public void Populate(JSONNode jsonNode)
        {
            if (jsonNode["title"])
            {
                Title = jsonNode["title"];
            }

            if (jsonNode["allowUserSettings"] != null)
            {
                AllowUserSettings = jsonNode["allowUserSettings"].AsBool;
            }

            if (jsonNode["corsProxyUrl"] != null)
            {
                corsProxyUrl = jsonNode["corsProxyUrl"];
            }

            if (jsonNode["projectUrl"] != null)
            {
                projectUrl = jsonNode["projectUrl"];
            }

            Origin = new Coordinate(
                jsonNode["origin"]["epsg"],
                jsonNode["origin"]["x"],
                jsonNode["origin"]["y"],
                jsonNode["origin"]["z"]
            );
            Debug.Log($"Set origin '{Origin}' from Configuration file");

            foreach (var element in jsonNode["functionalities"])
            {
                var functionality = Functionalities.FirstOrDefault(functionality => functionality.Id == element.Key);
                if (!functionality) continue;

                functionality.Populate(element.Value);
                if (functionality.IsEnabled) Debug.Log($"Enabled functionality '{functionality.Id}' from Configuration file");
            }
        }

        public void Populate(NameValueCollection queryParameters)
        {
            if (UrlContainsConfiguration(queryParameters))
            {
                ShouldStartSetup = false;
            }

            var originFromQueryString = queryParameters.Get("origin");
            if (string.IsNullOrEmpty(originFromQueryString) == false)
            {
                LoadOriginFromString(originFromQueryString);
            }

            var projectUrlFromQueryString = queryParameters.Get("project");
            if (string.IsNullOrEmpty(projectUrlFromQueryString) == false)
            {
                projectUrl = projectUrlFromQueryString;
            }

            var functionalitiesFromQueryString = queryParameters.Get("features") ?? queryParameters.Get("functionalities");
            if (functionalitiesFromQueryString != null)
            {
                LoadFunctionalitiesFromString(functionalitiesFromQueryString);
            }

            PopulateFunctionalitySpecificConfigurations(queryParameters);
        }

        private void PopulateFunctionalitySpecificConfigurations(NameValueCollection queryParameters)
        {
            foreach (var functionality in Functionalities)
            {
                var config = functionality.configuration as IConfiguration;
                if (config == null) continue;

                config.Populate(queryParameters);
                if (config.Validate().Count > 0 && functionality.IsEnabled)
                {
                    functionality.IsEnabled = false;
                }
            }
        }

        public string ToQueryString()
        {
            var uriBuilder = new UriBuilder();
            AddQueryParameters(uriBuilder);

            return uriBuilder.Uri.Query;
        }

        public void AddQueryParameters(UriBuilder urlBuilder)
        {
            var enabledfunctionalities = Functionalities.Where(functionality => functionality.IsEnabled).Select(functionality => functionality.Id);

            var originRDNAP = origin.Convert(CoordinateSystem.RDNAP);
            urlBuilder.AddQueryParameter("origin", $"{(int)originRDNAP.value1},{(int)originRDNAP.value2},{(int)originRDNAP.value3}");
            urlBuilder.AddQueryParameter("functionalities", string.Join(',', enabledfunctionalities.ToArray()));
            foreach (var functionality in Functionalities)
            {
                if (functionality.configuration is not IConfiguration functionalityConfiguration) continue;

                functionalityConfiguration.AddQueryParameters(urlBuilder);
            }
        }

        public List<string> Validate()
        {
            return new List<string>();
        }

        public JSONNode ToJsonNode()
        {
            var result = new JSONObject
            {
                ["title"] = Title,
                ["allowUserSettings"] = AllowUserSettings,
                ["origin"] = new JSONObject()
                {
                    ["epsg"] = origin.CoordinateSystem,
                    ["x"] = origin.value1,
                    ["y"] = origin.value2,
                    ["z"] = origin.value3,
                },
                ["functionalities"] = new JSONObject()
            };

            foreach (var functionality in Functionalities)
            {
                result["functionalities"][functionality.Id] = functionality.ToJsonNode();
            }

            return result;
        }

        private bool UrlContainsConfiguration(NameValueCollection queryParameters)
        {
            string origin = queryParameters.Get("origin");
            string functionalities = queryParameters.Get("features") ?? queryParameters.Get("functionalities");

            return origin != null && functionalities != null;
        }

        private void LoadOriginFromString(string origin)
        {
            var originParts = origin.Split(',');
            int.TryParse(originParts[0].Trim(), out int x);
            int.TryParse(originParts[1].Trim(), out int y);
            int.TryParse(originParts[2].Trim(), out int z);

            Origin = new Coordinate(CoordinateSystem.RDNAP, x, y, z);
            CoordinateSystems.SetOrigin(Origin);
            Debug.Log($"Set origin '{Origin}' from URL");
        }

        private void LoadFunctionalitiesFromString(string functionalities)
        {
            var functionalityIdentifiers = functionalities.ToLower().Split(',');
            foreach (var functionality in Functionalities)
            {
                functionality.IsEnabled = functionalityIdentifiers.Contains(functionality.Id);
                if (functionality.IsEnabled) Debug.Log($"Enabled functionality '{functionality.Id}' from URL");
            }
        }

        private void LoadFunctionalitiesFromProject(ProjectData changedData)
        {
            foreach (var savedData in changedData.functionalities)
            {
                var savedId = savedData.Id;
                var matchingFunctionality = Functionalities.FirstOrDefault(f => f.Data.Id == savedId);
                if (matchingFunctionality != null)
                {
                    matchingFunctionality.Data = savedData;
                    matchingFunctionality.IsEnabled = savedData.IsEnabled;
                }
            }

            AddFunctionalityDataToProject(); //re add missing functionalities to project, in case the project did not have a specific functionalityData
        }

        public void AddFunctionalityDataToProject()
        {
            foreach (var functionality in Functionalities)
            {
                ProjectData.Current.AddFunctionality(functionality.Data);
            }
        }

        public void AddProjectDataChangedListener()
        {
            ProjectData.Current.OnDataChanged.AddListener(LoadFunctionalitiesFromProject);
        }
    }
}