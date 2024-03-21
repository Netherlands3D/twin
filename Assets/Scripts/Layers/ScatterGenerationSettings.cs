using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(fileName = "TreeGenerationSettings", menuName = "ScriptableObjects/TreeGenerationSettings", order = 1)]
    [Serializable]
    public class ScatterGenerationSettings : ScriptableObject, IPropertySectionInstantiator
    {
        [SerializeField] private float density = 1f;
        [SerializeField] private float scatter = 0f;
        [SerializeField] private float angle = 0f;
        [SerializeField] private Vector3 minScale = Vector3.one;
        [SerializeField] private Vector3 maxScale = Vector3.one;
        [SerializeField] private FillType fillType = FillType.Complete;
        [SerializeField] private float strokeWidth = 1f;

        public UnityEvent SettingsChanged = new UnityEvent();
        
        public float Density
        {
            get { return density; }
            set
            {
                density = value;
                SettingsChanged.Invoke();
            }
        }

        public float Scatter
        {
            get { return scatter; }
            set
            {
                scatter = value;
                SettingsChanged.Invoke();
            }
        }

        public float Angle
        {
            get { return angle; }
            set
            {
                angle = value;
                SettingsChanged.Invoke();
            }
        }
        
        public Vector3 MinScale
        {
            get { return minScale; }
            set
            {
                minScale = value;
                SettingsChanged.Invoke();
            }
        }
        
        public Vector3 MaxScale
        {
            get { return maxScale; }
            set
            {
                maxScale = value;
                SettingsChanged.Invoke();
            }
        }
        
        public FillType FillType
        {
            get => fillType;
            set
            {
                fillType = value;
                SettingsChanged.Invoke();
            } 
        } 
        public float StrokeWidth
        {
            get => strokeWidth;
            set
            {
                strokeWidth = value;
                SettingsChanged.Invoke();
            }
        }
        
        public Vector3 GenerateRandomScale()
        {
            float x = UnityEngine.Random.Range(minScale.x, maxScale.x);
            float y = UnityEngine.Random.Range(minScale.y, maxScale.y);
            float z = UnityEngine.Random.Range(minScale.z, maxScale.z);

            return new Vector3(x, y, z);
        }
        
        public void AddToProperties(RectTransform properties)
        {
            var propertySection = Instantiate(ScatterMap.Instance.propertyPanelPrefab, properties);
            propertySection.Settings = this;
        }
    }
}