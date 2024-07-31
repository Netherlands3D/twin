using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.ObjectLibrary
{
    [RequireComponent(typeof(LayerGameObject))]
    public class Windmill : MonoBehaviour, ILayerWithPropertyData
    {
        public float RotorDiameter
        {
            get => propertyData.RotorDiameter;
            set => propertyData.RotorDiameter = value;
        }
        private WindmillPropertyData propertyData;
        public LayerPropertyData PropertyData => propertyData;

        public float AxisHeight
        {
            get => propertyData.AxisHeight;
            set => propertyData.AxisHeight = value;
        }
        // public WindmillPropertyData PropertyData => propertyData;

        [Header("Settings")] [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float defaultHeight = 120f;
        [SerializeField] private float defaultDiameter = 120f;

        [Header("Models")] [SerializeField] private Transform windmillBase;
        [SerializeField] private Transform windmillAxis;
        [SerializeField] private Transform windmillRotor;
        [SerializeField] private Transform[] windmillBlades;

        [SerializeField] private float baseModelHeight = 1.679f;
        [SerializeField] private float baseModelDiameter = 0.79405f;
        [SerializeField] private float rotorModelLength = 13.2f;
        [SerializeField] private float basePercentage = 0.1f;

        private MeshRenderer baseRenderer;

        private void Awake()
        {
            baseRenderer = windmillBase.GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            UpdateAxisHeight(propertyData.AxisHeight);
            UpdateRotorDiameter(propertyData.RotorDiameter);
        }


        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var windmillProperties = (WindmillPropertyData)properties.FirstOrDefault(p => p is WindmillPropertyData);
            if (windmillProperties != null)
            {
                propertyData = windmillProperties;
                print("LOADED RD: " + propertyData.RotorDiameter);
                print("LOADED AH: " + propertyData.AxisHeight);
            }

            if (propertyData == null)
            {
                propertyData = new()
                {
                    AxisHeight = defaultHeight,
                    RotorDiameter = defaultDiameter
                };
            }

            AddListeners();
        }

        private void AddListeners()
        {
            propertyData.OnAxisHeightChanged.AddListener(UpdateAxisHeight);
            propertyData.OnRotorDiameterChanged.AddListener(UpdateRotorDiameter);
        }

        private void RemoveListeners()
        {
            propertyData.OnAxisHeightChanged.RemoveListener(UpdateAxisHeight);
            propertyData.OnRotorDiameterChanged.RemoveListener(UpdateRotorDiameter);
        }

        private void UpdateAxisHeight(float height)
        {
            // if (height == 0)
            // {
            //     Debug.LogWarning($"Windmill {name} has no height, using fallback height");
            //     height = defaultHeight;
            // }

            var baseHeight = height / baseModelHeight;

            var baseScale = baseHeight * basePercentage;
            baseScale /= baseModelDiameter;

            windmillBase.localScale = new Vector3(baseScale, baseHeight, baseScale);
            windmillAxis.localScale = new Vector3(baseScale, baseScale, baseScale);

            var axisPosition = baseRenderer.bounds.size.y;
            windmillAxis.localPosition = new Vector3(0, axisPosition, 0);
        }

        private void UpdateRotorDiameter(float diameter)
        {
            // if (diameter == 0)
            // {
            //     Debug.LogWarning($"Windmill {name} has no diameter, using fallback diameter");
            //     diameter = defaultDiameter;
            // }

            var rotorsLength = diameter * 0.5f;
            //Scale the windmillRotors
            foreach (var windmillBlade in windmillBlades)
            {
                var rotorScale = rotorsLength / windmillAxis.localScale.x;
                rotorScale /= rotorModelLength;

                windmillBlade.localScale = new Vector3(rotorScale, rotorScale, rotorScale);
            }
        }

        private void Update()
        {
            windmillRotor.Rotate(Vector3.forward, Time.deltaTime * rotationSpeed, Space.Self);
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }
    }
}