using System;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Functionalities
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Functionality", fileName = "Functionality", order = 0)]
    public class Functionality : ScriptableObject, ISimpleJsonMapper
    {
        [SerializeField] private FunctionalityData data = new();

        public FunctionalityData Data
        {
            get { return data; }
            set
            {
                var oldEnabled = data.IsEnabled;
                data = value;

                //invoke events if the state of the new Data object and old Data object don't match
                if (data.IsEnabled == oldEnabled)
                    return;
                
                InvokeOnEnableChangeEvents();
            }
        }

        [Tooltip("Functionality button title")]
        public string Title;

        [Tooltip("Functionality button caption")]
        public string Caption;

        [Tooltip("The header above the description")]
        public string Header;

        [TextArea(5, 10)] public string Description;
        public ScriptableObject configuration;

        public string Id => Data.Id;

        public bool IsEnabled
        {
            get => Data.IsEnabled;
            set
            {
                //cant enable functionality if configuration is invalid
                if (Data.IsEnabled && configuration is IConfiguration config && config.Validate().Count > 0)
                {
                    Debug.LogWarning($"Can't enable functionality {Title} because configuration is invalid");
                }

                if (value == Data.IsEnabled) //IsEnabled was not changed
                    return;

                Data.IsEnabled = value;
                
                InvokeOnEnableChangeEvents();
            }
        }

        public UnityEvent OnEnable = new();
        public UnityEvent OnDisable = new();

        private void InvokeOnEnableChangeEvents()
        {
            if (Data.IsEnabled)
                OnEnable.Invoke();
            else
                OnDisable.Invoke();
        }
        
        public void Populate(JSONNode jsonNode)
        {
            IsEnabled = jsonNode["enabled"];
            (configuration as IConfiguration)?.Populate(jsonNode["configuration"]);
        }

        public JSONNode ToJsonNode()
        {
            return new JSONObject
            {
                ["enabled"] = Data.IsEnabled,
                ["configuration"] = (configuration as IConfiguration)?.ToJsonNode()
            };
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(Data.Id))
            {
                Data.Id = Title.ToLower().Replace(" ", "-");
            }
        }
    }
}