using System;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Features
{
    [Serializable]
    public class FeatureLink
    {
        [HideInInspector]
        public string name;
        public Feature feature;
        public UnityEvent<bool> onFeatureToggle;
    }
}