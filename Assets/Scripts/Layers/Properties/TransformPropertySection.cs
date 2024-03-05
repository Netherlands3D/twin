using System;
using System.Globalization;
using Netherlands3D.Coordinates;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class TransformPropertySection : AbstractHierarchicalObjectPropertySection
    {
        private HierarchicalObjectLayer layer;

        [Serializable]
        private class SetOfXYZ
        {
            public TMP_InputField xField;
            public TMP_InputField yField;
            public TMP_InputField zField;
        }

        [SerializeField] private SetOfXYZ position = new();
        [SerializeField] private SetOfXYZ rotation = new();
        [SerializeField] private SetOfXYZ scale = new();

        public override HierarchicalObjectLayer Layer
        {
            get => layer;
            set
            {
                layer = value;
                UpdatePositionFields();
                UpdateRotationFields();
                UpdateScalingFields();
            }
        }
        
        private void Awake()
        {
            position.xField.onSubmit.AddListener(OnPositionChanged);
            position.yField.onSubmit.AddListener(OnPositionChanged);
            position.zField.onSubmit.AddListener(OnPositionChanged);
            rotation.xField.onSubmit.AddListener(OnRotationChanged);
            rotation.yField.onSubmit.AddListener(OnRotationChanged);
            rotation.zField.onSubmit.AddListener(OnRotationChanged);
            scale.xField.onSubmit.AddListener(OnScaleChanged);
            scale.yField.onSubmit.AddListener(OnScaleChanged);
            scale.zField.onSubmit.AddListener(OnScaleChanged);
        }

        private void Update()
        {
            if (layer.transform.hasChanged)
            {
                UpdatePositionFields();
                UpdateRotationFields();
                UpdateScalingFields();
                Layer.transform.hasChanged = false;
            }
        }

        private void OnPositionChanged(string axisValue)
        {
            double.TryParse(position.xField.text, out var x);
            double.TryParse(position.yField.text, out var y);
            double.TryParse(position.zField.text, out var z);
            
            var rdCoordinate = new Coordinate(CoordinateSystem.RD, x, y, z);

            var unityCoordinate = CoordinateConverter.ConvertTo(rdCoordinate, CoordinateSystem.Unity).ToVector3();

            layer.transform.position = unityCoordinate;
        }
        
        private void OnRotationChanged(string axisValue)
        {
            float.TryParse(rotation.xField.text, out var x);
            float.TryParse(rotation.yField.text, out var y);
            float.TryParse(rotation.zField.text, out var z);

            layer.transform.eulerAngles = new Vector3(x, y, z);
        }
        
        private void OnScaleChanged(string axisValue)
        {
            float.TryParse(scale.xField.text, out var x);
            float.TryParse(scale.yField.text, out var y);
            float.TryParse(scale.zField.text, out var z);

            layer.transform.localScale = new Vector3(x, y, z);
        }
        
        private void UpdatePositionFields()
        {
            var rdCoordinate = ConvertLayerPositionToRd(layer);
            position.xField.SetTextWithoutNotify(rdCoordinate.Points[0].ToString("0", CultureInfo.InvariantCulture));
            position.yField.SetTextWithoutNotify(rdCoordinate.Points[1].ToString("0", CultureInfo.InvariantCulture));
            position.zField.SetTextWithoutNotify(rdCoordinate.Points[2].ToString("0", CultureInfo.InvariantCulture));
        }

        private void UpdateRotationFields()
        {
            var eulerAngles = layer.transform.localEulerAngles;
            rotation.xField.SetTextWithoutNotify(eulerAngles.x.ToString("0.00", CultureInfo.InvariantCulture));
            rotation.yField.SetTextWithoutNotify(eulerAngles.y.ToString("0.00", CultureInfo.InvariantCulture));
            rotation.zField.SetTextWithoutNotify(eulerAngles.z.ToString("0.00", CultureInfo.InvariantCulture));
        }

        private void UpdateScalingFields()
        {
            var localScale = layer.transform.localScale;
            scale.xField.SetTextWithoutNotify(localScale.x.ToString("0.00", CultureInfo.InvariantCulture));
            scale.yField.SetTextWithoutNotify(localScale.y.ToString("0.00", CultureInfo.InvariantCulture));
            scale.zField.SetTextWithoutNotify(localScale.z.ToString("0.00", CultureInfo.InvariantCulture));
        }

        private Coordinate ConvertLayerPositionToRd(HierarchicalObjectLayer origin)
        {
            var transformPosition = origin.transform.position;
            var unityCoordinate = new Coordinate(
                CoordinateSystem.Unity, 
                transformPosition.x, 
                transformPosition.y,
                transformPosition.z
            );

            return CoordinateConverter.ConvertTo(unityCoordinate, CoordinateSystem.RD);
        }
    }
}