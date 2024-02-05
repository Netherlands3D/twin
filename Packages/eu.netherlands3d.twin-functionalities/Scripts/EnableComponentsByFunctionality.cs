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

        [Header("Centralized list with Functionalities and their connections to GameObjects")]
        [FormerlySerializedAs("FeatureLinks")]
        public List<FunctionalityLink> FunctionalityLinks = new();

        private void Awake()
        {
            gameObject.SetActive(false);

            foreach (var functionalityLink in FunctionalityLinks)
            {
                AddFunctionalityListenerForLink(functionalityLink);
            }

            gameObject.SetActive(true);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (var functionalityLink in FunctionalityLinks)
            {
                var linkFunctionality = functionalityLink.functionality != null ? functionalityLink.functionality : null;
                functionalityLink.name = linkFunctionality?.Caption + " -> ";

                int listenerCount = functionalityLink.onFunctionalityToggle?.GetPersistentEventCount() ?? 0;
                
                if (listenerCount == 1)
                {
                    var type = functionalityLink.onFunctionalityToggle.GetPersistentTarget(0).ToString().Replace("(UnityEngine.", "(");
                    var methodName = functionalityLink.onFunctionalityToggle.GetPersistentMethodName(0);
                    functionalityLink.name +=  type + " -> " + methodName;
                }
                else if (listenerCount > 1)
                {
                    functionalityLink.name += " (" + listenerCount + ")";
                }
                else{
                    functionalityLink.name += " No listeners";
                }
            }
        }
#endif

        private void AddFunctionalityListenerForLink(FunctionalityLink functionalityLink)
        {
            FunctionalityListener listener = gameObject.AddComponent<FunctionalityListener>();
            listener.functionality = functionalityLink.functionality;
            listener.OnEnableFunctionality.AddListener((Functionality functionality) =>
            {
                functionalityLink.onFunctionalityToggle?.Invoke(true);
            });
            listener.OnDisableFunctionality.AddListener((Functionality functionality) =>
            {
                functionalityLink.onFunctionalityToggle?.Invoke(false);
            });
        }
    }
}
