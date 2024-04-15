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

        public UnityEvent ScatterSettingsChanged = new UnityEvent(); //called when the settings of the to be scattered objects change, without needing to regenerate the sampler texture
        public UnityEvent ScatterShapeChanged = new UnityEvent(); //called when the settings of the shape should change, thereby needing a regenerating of the sampler texture

        public bool AutoRotateToLine { get; set; } = false;

        public float Density
        {
            get { return density; }
            set
            {
                if (density == value)
                    return;

                density = value;
                ScatterShapeChanged.Invoke(); //changing the density requires a rerender of the shape because of the resolution change
            }
        }

        public float Scatter
        {
            get { return scatter; }
            set
            {
                if (scatter == value)
                    return;

                scatter = value;
                ScatterSettingsChanged.Invoke();
            }
        }

        public float Angle
        {
            get { return angle; }
            set
            {
                if (angle == value)
                    return;

                angle = value;
                ScatterSettingsChanged.Invoke();
            }
        }

        public Vector3 MinScale
        {
            get { return minScale; }
            set
            {
                if (minScale == value)
                    return;

                minScale = value;
                ScatterSettingsChanged.Invoke();
            }
        }

        public Vector3 MaxScale
        {
            get { return maxScale; }
            set
            {
                if (maxScale == value)
                    return;

                maxScale = value;
                ScatterSettingsChanged.Invoke();
            }
        }

        public FillType FillType
        {
            get => fillType;
            set
            {
                if (fillType == value)
                    return;

                fillType = value;
                ScatterShapeChanged.Invoke();
            }
        }

        public float StrokeWidth
        {
            get => strokeWidth;
            set
            {
                if (strokeWidth == value)
                    return;

                strokeWidth = value;
                ScatterShapeChanged.Invoke();
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