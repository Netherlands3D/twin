using Netherlands3D.Coordinates;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class TransformPropertySection : AbstractHierarchicalObjectPropertySection
    {
        private HierarchicalObjectLayer layer;
        [SerializeField] private TMP_InputField xField;
        [SerializeField] private TMP_InputField yField;
        [SerializeField] private TMP_InputField zField;

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
            xField.onSubmit.AddListener(OnPositionChanged);
            yField.onSubmit.AddListener(OnPositionChanged);
            zField.onSubmit.AddListener(OnPositionChanged);
        }

        private void Update()
        {
            UpdatePositionFields();
        }

        private void OnPositionChanged(string axisValue)
        {
            double.TryParse(xField.text, out var x);
            double.TryParse(yField.text, out var y);
            double.TryParse(zField.text, out var z);
            
            var rdCoordinate = new Coordinate(CoordinateSystem.RD, x, y, z);

            var unityCoordinate = CoordinateConverter.ConvertTo(rdCoordinate, CoordinateSystem.RD).ToVector3();

            layer.transform.position = unityCoordinate;
        }
        
        private void UpdatePositionFields()
        {
            var rdCoordinate = ConvertLayerPositionToRd(layer);
            xField.SetTextWithoutNotify(rdCoordinate.Points[0].ToString("G"));
            yField.SetTextWithoutNotify(rdCoordinate.Points[1].ToString("G"));
            zField.SetTextWithoutNotify(rdCoordinate.Points[2].ToString("G"));
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