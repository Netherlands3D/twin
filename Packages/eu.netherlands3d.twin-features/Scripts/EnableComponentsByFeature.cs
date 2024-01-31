using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Features
{
    public class EnableComponentsByFunctionality : MonoBehaviour
    {
        public List<FunctionalityLink> FunctionalityLinks = new();

        private void Awake()
        {
            gameObject.SetActive(false);
            try
            {
                foreach (var functionalityLink in FunctionalityLinks)
                {
                    AddFeatureListenerForLink(functionalityLink);
                }
            }
            finally
            {
                gameObject.SetActive(true);
            }
        }

        private void OnValidate()
        {
            foreach (var featureLink in FunctionalityLinks)
            {
                var linkFeature = featureLink.feature != null ? featureLink.feature : null;
                featureLink.name = linkFeature?.Caption + " -> ";

                int listenerCount = featureLink.onFeatureToggle?.GetPersistentEventCount() ?? 0;
                
                if (listenerCount == 1)
                {
                    var type = featureLink.onFeatureToggle.GetPersistentTarget(0).ToString().Replace("(UnityEngine.", "(");
                    var methodName = featureLink.onFeatureToggle.GetPersistentMethodName(0);
                    featureLink.name +=  type + " -> " + methodName;
                }
                else if (listenerCount > 1)
                {
                    featureLink.name += " (" + listenerCount + ")";
                }
                else{
                    featureLink.name += " No listeners";
                }
            }
        }


        private void AddFeatureListenerForLink(FunctionalityLink featureLink)
        {
            FeatureListener listener = gameObject.AddComponent<FeatureListener>();
            listener.feature = featureLink.feature;
            listener.OnEnableFeature.AddListener((Functionality feature) =>
            {
                featureLink.onFeatureToggle?.Invoke(true);
            });
            listener.OnDisableFeature.AddListener((Functionality feature) =>
            {
                featureLink.onFeatureToggle?.Invoke(false);
            });
        }
    }
}
