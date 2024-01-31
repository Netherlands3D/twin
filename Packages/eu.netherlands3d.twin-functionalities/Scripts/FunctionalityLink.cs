using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Functionalities
{
    [Serializable]
    public class FunctionalityLink
    {
        [HideInInspector]
        public string name;
        public Functionality feature;

        [FormerlySerializedAs("onFeatureToggle")]
        public UnityEvent<bool> onFeatureToggle = new();
    }
}