using System;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Features
{
    [Serializable]
    public class FunctionalityLink
    {
        [HideInInspector]
        public string name;
        public Functionality feature;
        public UnityEvent<bool> onFeatureToggle = new();
    }
}