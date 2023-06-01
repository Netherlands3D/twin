using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Features
{
    public class FeatureListener : MonoBehaviour
    {
        public Feature feature;

        public UnityEvent<Feature> OnEnableFeature = new ();
        public UnityEvent<Feature> OnDisableFeature = new ();

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

            feature.OnEnable.AddListener(EnableFeature);
            feature.OnDisable.AddListener(DisableFeature);
        }

        private void EnableFeature()
        {
            OnEnableFeature.Invoke(feature);
        }

        private void DisableFeature()
        {
            OnDisableFeature.Invoke(feature);
        }

        private void OnDisable()
        {
            feature.OnEnable.RemoveListener(EnableFeature);
            feature.OnDisable.RemoveListener(DisableFeature);
        }
    }
}