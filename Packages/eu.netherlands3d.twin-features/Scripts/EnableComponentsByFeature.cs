using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Features
{
    public class EnableComponentsByFeature : MonoBehaviour
    {
        public List<FeatureLink> FeatureLinks = new();

        private void OnValidate()
        {
            foreach (var featureLink in FeatureLinks)
            {
                var linkFeature = featureLink.feature != null ? featureLink.feature : null;
                var linkComponent = featureLink.component != null ? featureLink.component : null;
                featureLink.name = linkFeature?.Caption + " -> " + linkComponent?.name;
                switch (featureLink.action)
                {
                    case FeatureLinkAction.ToggleGameObject:
                        featureLink.name += " (GameObject)";
                        break;
                    case FeatureLinkAction.ToggleComponent:
                        featureLink.name += $" ({featureLink.component.GetType()})";
                        break;
                }
            }
        }

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
                var linkComponent = featureLink.component;
                switch (featureLink.action)
                {
                    case FeatureLinkAction.ToggleGameObject:
                        linkComponent.gameObject.SetActive(true);
                        break;
                    case FeatureLinkAction.ToggleComponent:
                        linkComponent.enabled = true;
                        break;
                }
            });
            listener.OnDisableFeature.AddListener((Feature feature) =>
            {
                var linkComponent = featureLink.component;
                switch (featureLink.action)
                {
                    case FeatureLinkAction.ToggleGameObject:
                        linkComponent.gameObject.SetActive(false);
                        break;
                    case FeatureLinkAction.ToggleComponent:
                        linkComponent.enabled = false;
                        break;
                }
            });
        }
    }
}
