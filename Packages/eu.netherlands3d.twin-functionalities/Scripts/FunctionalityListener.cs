using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Functionalities
{
    public class FeatureListener : MonoBehaviour
    {
        public Functionality feature;

        public UnityEvent<Functionality> OnEnableFeature = new ();
        public UnityEvent<Functionality> OnDisableFeature = new ();

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