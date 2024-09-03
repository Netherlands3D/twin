using System;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Functionalities
{
    [Serializable]
    public class FunctionalityData
    {
        public string Id;
        public bool IsEnabled;
    }
    
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Functionality", fileName = "Functionality", order = 0)]
    public class Functionality : ScriptableObject, ISimpleJsonMapper
    {
        [JsonProperty] public FunctionalityData Data = new();
        public string Id; //todo delete
        
        [Tooltip("Functionality button title"), JsonIgnore]
        public string Title;

        [Tooltip("Functionality button caption"), JsonIgnore]
        public string Caption;

        [Tooltip("The header above the description"), JsonIgnore]
        public string Header;
        [TextArea(5, 10), JsonIgnore]
        public string Description;
        [JsonIgnore] public ScriptableObject configuration;

        [SerializeField] private bool isEnabled;//todo delete
        
        [JsonProperty]
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                var config = configuration as IConfiguration;
                var targetValue = value;

                //cant enable functionality if configuration is invalid
                if (isEnabled && config != null && config.Validate().Count > 0){
                    Debug.LogWarning($"Can't enable functionality {Title} because configuration is invalid");
                    targetValue = false;
                }

                var wasEnabled = isEnabled;
                isEnabled = value;
                Debug.Log( Id+" setting func act: " + value);
                switch (wasEnabled)
                {
                    case false when isEnabled:
                        OnEnable.Invoke();
                        break;
                    case true when isEnabled == false:
                        OnDisable.Invoke();
                        break;
                }
            }
        }
        
        [JsonIgnore] public UnityEvent OnEnable = new();
        [JsonIgnore] public UnityEvent OnDisable = new();
        
        public void Populate(JSONNode jsonNode)
        {
            IsEnabled = jsonNode["enabled"];
            (configuration as IConfiguration)?.Populate(jsonNode["configuration"]);
        }

        public JSONNode ToJsonNode()
        {
            return new JSONObject
            {
                ["enabled"] = isEnabled,
                ["configuration"] = (configuration as IConfiguration)?.ToJsonNode()
            };
        }

        private void OnValidate() {
            if(string.IsNullOrEmpty(Id)) {
                Id = Title.ToLower().Replace(" ", "-");
            }
        }
    }
}