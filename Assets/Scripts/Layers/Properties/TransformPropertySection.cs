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

        private const string percentageCharacter = "%";

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
            position.xField.onEndEdit.AddListener(OnPositionChanged);
            position.yField.onEndEdit.AddListener(OnPositionChanged);
            position.zField.onEndEdit.AddListener(OnPositionChanged);
            rotation.xField.onEndEdit.AddListener(OnRotationChanged);
            rotation.yField.onEndEdit.AddListener(OnRotationChanged);
            rotation.zField.onEndEdit.AddListener(OnRotationChanged);        
            scale.xField.onValueChanged.AddListener(OnScaleChanged);
            scale.yField.onValueChanged.AddListener(OnScaleChanged);
            scale.zField.onValueChanged.AddListener(OnScaleChanged);
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
            float.TryParse(scale.xField.text.Replace(percentageCharacter,""), out var x);
            float.TryParse(scale.yField.text.Replace(percentageCharacter,""), out var y);
            float.TryParse(scale.zField.text.Replace(percentageCharacter,""), out var z);

            UpdateScalingFields();

            layer.transform.localScale = new Vector3(x / 100.0f, y / 100.0f, z / 100.0f);
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

            var xPercentage = localScale.x * 100;
            var yPercentage = localScale.y * 100;
            var zPercentage = localScale.z * 100;

            scale.xField.SetTextWithoutNotify($"{xPercentage.ToString("0", CultureInfo.InvariantCulture)}{percentageCharacter}");
            scale.yField.SetTextWithoutNotify($"{yPercentage.ToString("0", CultureInfo.InvariantCulture)}{percentageCharacter}");
            scale.zField.SetTextWithoutNotify($"{zPercentage.ToString("0", CultureInfo.InvariantCulture)}{percentageCharacter}");
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