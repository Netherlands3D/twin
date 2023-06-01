using System;
using UnityEngine;

namespace Netherlands3D.Twin.Features
{
    [Serializable]
    public class FeatureLink
    {
        [HideInInspector]
        public string name;
        public Feature feature;
        public MonoBehaviour component;
        public FeatureLinkAction action;
    }
}