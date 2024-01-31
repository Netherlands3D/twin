using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Functionalities
{
    public class EnableComponentsByFunctionality : MonoBehaviour
    {
        [Serializable]
        public class FunctionalityLink
        {
            [HideInInspector]
            public string name;
            public Functionality functionality;
            public UnityEvent<bool> onFunctionalityToggle = new();
        }

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
                var linkFeature = featureLink.functionality != null ? featureLink.functionality : null;
                featureLink.name = linkFeature?.Caption + " -> ";

                int listenerCount = featureLink.onFunctionalityToggle?.GetPersistentEventCount() ?? 0;
                
                if (listenerCount == 1)
                {
                    var type = featureLink.onFunctionalityToggle.GetPersistentTarget(0).ToString().Replace("(UnityEngine.", "(");
                    var methodName = featureLink.onFunctionalityToggle.GetPersistentMethodName(0);
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
            FunctionalityListener listener = gameObject.AddComponent<FunctionalityListener>();
            listener.functionality = featureLink.functionality;
            listener.OnEnableFeature.AddListener((Functionality feature) =>
            {
                featureLink.onFunctionalityToggle?.Invoke(true);
            });
            listener.OnDisableFeature.AddListener((Functionality feature) =>
            {
                featureLink.onFunctionalityToggle?.Invoke(false);
            });
        }
    }
}
