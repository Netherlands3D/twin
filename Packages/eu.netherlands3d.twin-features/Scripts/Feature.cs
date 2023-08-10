using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Features
{
    [CreateAssetMenu(menuName = "Netherlands3D/Twin/Feature", fileName = "Feature", order = 0)]
    public class Feature : ScriptableObject
    {
        public string Id;
        public string Caption;
        
        [JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
        public ScriptableObject configuration;

        [SerializeField] private bool isEnabled;

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
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
    }
}