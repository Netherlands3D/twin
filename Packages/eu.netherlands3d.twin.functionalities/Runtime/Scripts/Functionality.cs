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

        public FunctionalityData Data
        {
            get => currentData ??= CreateDefaultData();
            set
            {
                var oldEnabled = Data.IsEnabled;
                currentData = value;

                if (currentData.IsEnabled != oldEnabled)
                    InvokeOnEnableChangeEvents();
            }
        }

        public void ResetDataToDefaults()
        {
            var data = CreateDefaultData();
            if (Type == FunctionalityType.Hidden)
                data.IsEnabled = IsEnabled;

            Data = data;
        }

        private FunctionalityData CreateDefaultData()
        {
            var data = defaultData.CreateCopy();
            if (Type == FunctionalityType.Hidden)
                data.IsEnabled = false;

            return data;
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

        public UnityEvent OnEnableFunctionality = new();
        public UnityEvent OnDisableFunctionality = new();

        private void InvokeOnEnableChangeEvents()
        {
            if (Data.IsEnabled)
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
                ["enabled"] = Data.IsEnabled,
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
