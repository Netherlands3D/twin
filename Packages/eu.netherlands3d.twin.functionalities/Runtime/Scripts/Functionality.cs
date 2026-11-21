using System;
using Netherlands3D.SimpleJSON;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Functionalities
{
    public enum FunctionalityType
    {
        Optional = 0, // Existing assets without a type are optional.
        Experimental,
        Hidden,
        Unsaved
    }

    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Functionality/Generic", fileName = "Functionality", order = 0)]
    public class Functionality : ScriptableObject, ISimpleJsonMapper
    {
        [SerializeReference, FormerlySerializedAs("data")]
        private FunctionalityData defaultData = new();

        [NonSerialized]
        private FunctionalityData currentData;

        [NonSerialized]
        private bool hiddenFeaturesAllowed;

        /// <summary>
        /// Allows a hidden functionality to be enabled by the spoke configuration or a temporary test override.
        /// </summary>
        public bool HiddenFeaturesAllowed
        {
            get => hiddenFeaturesAllowed;
            set
            {
                if (hiddenFeaturesAllowed == value) return;

                var wasEnabled = IsEnabled;
                hiddenFeaturesAllowed = value;
                if (Type == FunctionalityType.Hidden && !value)
                    Data.IsEnabled = false;

                if (IsEnabled != wasEnabled)
                    InvokeOnEnableChangeEvents();
            }
        }

        public FunctionalityData Data
        {
            get => currentData ??= CreateDefaultData();
            set
            {
                var oldEnabled = IsEnabled;
                currentData = value;

                if (IsEnabled != oldEnabled)
                    InvokeOnEnableChangeEvents();
            }
        }

        public void ResetDataToDefaults()
        {
            Data = CreateDefaultData();
        }

        private FunctionalityData CreateDefaultData()
        {
            return defaultData.CreateCopy();
        }

        [Tooltip("Functionality button title")]
        public string Title;

        [Tooltip("Functionality button caption")]
        public string Caption;

        [Tooltip("The header above the description")]
        public string Header;

        [TextArea(5, 10)] public string Description;
        public ScriptableObject configuration;
        public FunctionalityType Type = FunctionalityType.Optional;

        public string Id => defaultData.Id;

        public bool IsEnabled
        {
            get => Data.IsEnabled && (Type != FunctionalityType.Hidden || HiddenFeaturesAllowed);
            set
            {
                if (Type == FunctionalityType.Hidden && !HiddenFeaturesAllowed)
                    value = false;

                //cant enable functionality if configuration is invalid
                if (Data.IsEnabled && configuration is IConfiguration config && config.Validate().Count > 0)
                {
                    Debug.LogWarning($"Can't enable functionality {Title} because configuration is invalid");
                }

                if (value == Data.IsEnabled) //IsEnabled was not changed
                    return;

                var wasEnabled = IsEnabled;
                Data.IsEnabled = value;
                
                if (IsEnabled != wasEnabled)
                    InvokeOnEnableChangeEvents();
            }
        }

        public UnityEvent OnEnableFunctionality = new();
        public UnityEvent OnDisableFunctionality = new();

        private void InvokeOnEnableChangeEvents()
        {
            if (IsEnabled)
                OnEnableFunctionality.Invoke();
            else
                OnDisableFunctionality.Invoke();
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
                ["enabled"] = IsEnabled,
                ["configuration"] = (configuration as IConfiguration)?.ToJsonNode()
            };
        }
        
        protected virtual void OnEnable()
        {
            defaultData ??= new FunctionalityData();
        }
        
        protected virtual void OnValidate()
        {
            defaultData ??= new FunctionalityData();

            if (string.IsNullOrEmpty(defaultData.Id))
                defaultData.Id = Title.ToLower().Replace(" ", "-");

            if (!Application.isPlaying)
            {
                currentData = null;
            }
        }
        
        protected void EnsureDefaultDataType<T>()
            where T : FunctionalityData, new()
        {
            if (defaultData is T)
                return;

            var previousData = defaultData;

            defaultData = new T
            {
                Id = previousData?.Id,
                IsEnabled = previousData?.IsEnabled ?? false
            };
            
            currentData = null;
        }
    }
}
