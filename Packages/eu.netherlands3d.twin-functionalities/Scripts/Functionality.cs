using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Functionalities
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Functionality", fileName = "Functionality", order = 0)]
    public class Functionality : ScriptableObject, ISimpleJsonMapper
    {
        public string Id;

        [Tooltip("Functionality button title")]
        public string Title;

        [Tooltip("Functionality button caption")]
        public string Caption;

        [Tooltip("The header above the description")]
        public string Header;
        [TextArea(5, 10)]
        public string Description;
        public ScriptableObject configuration;

        [SerializeField] private bool isEnabled;

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

        public UnityEvent OnEnable = new();
        public UnityEvent OnDisable = new();

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