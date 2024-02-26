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
            }
        }
        
        private void Awake()
        {
            position.xField.onSubmit.AddListener(OnPositionChanged);
            position.yField.onSubmit.AddListener(OnPositionChanged);
            position.zField.onSubmit.AddListener(OnPositionChanged);
        }

        private void Update()
        {
            UpdatePositionFields();
            UpdateRotationFields();
        }

        private void OnPositionChanged(string axisValue)
        {
            double.TryParse(position.xField.text, out var x);
            double.TryParse(position.yField.text, out var y);
            double.TryParse(position.zField.text, out var z);
            
            var rdCoordinate = new Coordinate(CoordinateSystem.RD, x, y, z);

            var unityCoordinate = CoordinateConverter.ConvertTo(rdCoordinate, CoordinateSystem.RD).ToVector3();

            layer.transform.position = unityCoordinate;
        }
        
        private void UpdatePositionFields()
        {
            var rdCoordinate = ConvertLayerPositionToRd(layer);
            position.xField.SetTextWithoutNotify(rdCoordinate.Points[0].ToString("N0", CultureInfo.InvariantCulture));
            position.yField.SetTextWithoutNotify(rdCoordinate.Points[1].ToString("N0", CultureInfo.InvariantCulture));
            position.zField.SetTextWithoutNotify(rdCoordinate.Points[2].ToString("N0", CultureInfo.InvariantCulture));
        }

        private void UpdateRotationFields()
        {
            position.xField.SetTextWithoutNotify(transform.eulerAngles.x.ToString("N2", CultureInfo.InvariantCulture));
            position.yField.SetTextWithoutNotify(transform.eulerAngles.y.ToString("N2", CultureInfo.InvariantCulture));
            position.zField.SetTextWithoutNotify(transform.eulerAngles.z.ToString("N2", CultureInfo.InvariantCulture));
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