using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Features
{
    public class FeatureListener : MonoBehaviour
    {
        public Feature feature;

        public UnityEvent<Feature> OnEnableFeature = new ();
        public UnityEvent<Feature> OnDisableFeature = new ();

        private void Awake() {
            feature.OnEnable.AddListener(EnableFeature);
            feature.OnDisable.AddListener(DisableFeature);
        }
        
        private void OnEnable()
        {
            if (feature.IsEnabled)
            {
                EnableFeature();
            }
            else
            {
                DisableFeature();
            } 
        }

        private void EnableFeature()
        {
            OnEnableFeature.Invoke(feature);
        }

        private void DisableFeature()
        {
            OnDisableFeature.Invoke(feature);
        }

        private void OnDestroy()
        {
            feature.OnEnable.RemoveListener(EnableFeature);
            feature.OnDisable.RemoveListener(DisableFeature);
        }
    }
}