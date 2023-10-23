using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Features
{
    public class EnableComponentsByFeature : MonoBehaviour
    {
        public List<FeatureLink> FeatureLinks = new();

        private void Awake()
        {
            gameObject.SetActive(false);
            try
            {
                foreach (var featureLink in FeatureLinks)
                {
                    AddFeatureListenerForLink(featureLink);
                }
            }
            finally
            {
                gameObject.SetActive(true);
            }
        }

        private void AddFeatureListenerForLink(FeatureLink featureLink)
        {
            FeatureListener listener = gameObject.AddComponent<FeatureListener>();
            listener.feature = featureLink.feature;
            listener.OnEnableFeature.AddListener((Feature feature) =>
            {
                featureLink.onFeatureToggle?.Invoke(true);
            });
            listener.OnDisableFeature.AddListener((Feature feature) =>
            {
                featureLink.onFeatureToggle?.Invoke(false);
            });
        }
    }
}
