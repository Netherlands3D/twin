using System;
using System.Globalization;
using Netherlands3D.Coordinates;
using RuntimeHandle;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class TransformPropertySection : AbstractHierarchicalObjectPropertySection
    {
        private TransformLayerProperty transformProperty;
        private HierarchicalObjectLayerGameObject layerGameObject;

        [Serializable]
        private class SetOfXYZ
        {
            public FormField xField;
            public FormField yField;
            public FormField zField;
        }

        [SerializeField] private SetOfXYZ position = new();
        [SerializeField] private SetOfXYZ rotation = new();
        [SerializeField] private SetOfXYZ scale = new();

        private const string percentageCharacter = "%";

        public override HierarchicalObjectLayerGameObject LayerGameObject //todo: possibly remove this and replace it with a direct reference to the TransformLayerProperty
        {
            get => layerGameObject;
            set
            {
                layerGameObject = value;
                TransformProperty = layerGameObject.TransformProperty;
            } 
        }

        private TransformLayerProperty TransformProperty
        {
            get => transformProperty;
            set
            {
                if (transformProperty != null)
                {
                    TransformProperty.OnPositionChanged.RemoveListener(UpdatePositionFields);
                    TransformProperty.OnRotationChanged.RemoveListener(UpdateRotationFields);
                    TransformProperty.OnScaleChanged.RemoveListener(UpdateScalingFields);        
                }
                
                transformProperty = value;
                
                transformProperty.OnPositionChanged.AddListener(UpdatePositionFields);
                transformProperty.OnRotationChanged.AddListener(UpdateRotationFields);
                transformProperty.OnScaleChanged.AddListener(UpdateScalingFields);
                
                UpdatePositionFields(transformProperty.Position);
                UpdateRotationFields(transformProperty.EulerRotation);
                UpdateScalingFields(transformProperty.LocalScale);

                SetTransformLocks();
            }
        }
        
        private void OnEnable()
        {
            position.xField.onEndEdit.AddListener(OnPositionChanged);
            position.yField.onEndEdit.AddListener(OnPositionChanged);
            position.zField.onEndEdit.AddListener(OnPositionChanged);
            rotation.xField.onEndEdit.AddListener(OnRotationChanged);
            rotation.yField.onEndEdit.AddListener(OnRotationChanged);
            rotation.zField.onEndEdit.AddListener(OnRotationChanged);        
            scale.xField.onEndEdit.AddListener(OnScaleChanged);
            scale.yField.onEndEdit.AddListener(OnScaleChanged);
            scale.zField.onEndEdit.AddListener(OnScaleChanged);
        }

        private void OnDisable()
        {
            position.xField.onEndEdit.RemoveListener(OnPositionChanged);
            position.yField.onEndEdit.RemoveListener(OnPositionChanged);
            position.zField.onEndEdit.RemoveListener(OnPositionChanged);
            rotation.xField.onEndEdit.RemoveListener(OnRotationChanged);
            rotation.yField.onEndEdit.RemoveListener(OnRotationChanged);
            rotation.zField.onEndEdit.RemoveListener(OnRotationChanged);        
            scale.xField.onEndEdit.RemoveListener(OnScaleChanged);
            scale.yField.onEndEdit.RemoveListener(OnScaleChanged);
            scale.zField.onEndEdit.RemoveListener(OnScaleChanged);
            
            TransformProperty.OnPositionChanged.RemoveListener(UpdatePositionFields);
            TransformProperty.OnRotationChanged.RemoveListener(UpdateRotationFields);
            TransformProperty.OnScaleChanged.RemoveListener(UpdateScalingFields);        
        }

        // private void Update()
        // {
        //     if (layerGameObject.transform.hasChanged)
        //     {
        //         UpdatePositionFields();
        //         UpdateRotationFields();
        //         UpdateScalingFields();
        //         layerGameObject.transform.hasChanged = false;
        //     }
        // }

        private void SetTransformLocks()
        {
            if (layerGameObject.TryGetComponent(out TransformAxes transformLocks))
            {
                position.xField.Interactable = !transformLocks.PositionLocked && transformLocks.positionAxes.HasFlag(HandleAxes.X);
                position.yField.Interactable = !transformLocks.PositionLocked && transformLocks.positionAxes.HasFlag(HandleAxes.Y);
                position.zField.Interactable = !transformLocks.PositionLocked && transformLocks.positionAxes.HasFlag(HandleAxes.Z);
                rotation.xField.Interactable = !transformLocks.RotationLocked && transformLocks.rotationAxes.HasFlag(HandleAxes.X);
                rotation.yField.Interactable = !transformLocks.RotationLocked && transformLocks.rotationAxes.HasFlag(HandleAxes.Y);
                rotation.zField.Interactable = !transformLocks.RotationLocked && transformLocks.rotationAxes.HasFlag(HandleAxes.Z);
                scale.xField.Interactable = !transformLocks.ScaleLocked && transformLocks.scaleAxes.HasFlag(HandleAxes.X);
                scale.yField.Interactable = !transformLocks.ScaleLocked && transformLocks.scaleAxes.HasFlag(HandleAxes.Y);
                scale.zField.Interactable = !transformLocks.ScaleLocked && transformLocks.scaleAxes.HasFlag(HandleAxes.Z);
            }
            else
            {
                position.xField.Interactable = true;
                position.yField.Interactable = true;
                position.zField.Interactable = true;
                rotation.xField.Interactable = true;
                rotation.yField.Interactable = true;
                rotation.zField.Interactable = true;
                scale.xField.Interactable = true;
                scale.yField.Interactable = true;
                scale.zField.Interactable = true;
            }
        }

        private void OnPositionChanged(string axisValue)
        {
            double.TryParse(position.xField.Text, out var x);
            double.TryParse(position.yField.Text, out var y);
            double.TryParse(position.zField.Text, out var z);
            
            var rdCoordinate = new Coordinate(CoordinateSystem.RDNAP, x, y, z);
            TransformProperty.Position = rdCoordinate;
        }
        
        private void OnRotationChanged(string axisValue)
        {
            float.TryParse(rotation.xField.Text, out var x);
            float.TryParse(rotation.yField.Text, out var y);
            float.TryParse(rotation.zField.Text, out var z);

            TransformProperty.EulerRotation = new Vector3(x, y, z);
        }
        
        private void OnScaleChanged(string axisValue)
        {
            float.TryParse(scale.xField.Text.Replace(percentageCharacter,""), out var x);
            float.TryParse(scale.yField.Text.Replace(percentageCharacter,""), out var y);
            float.TryParse(scale.zField.Text.Replace(percentageCharacter,""), out var z);

            TransformProperty.LocalScale = new Vector3(x / 100.0f, y / 100.0f, z / 100.0f);
        }
        
        private void UpdatePositionFields(Coordinate coordinate)
        {
            var rdCoordinate = CoordinateConverter.ConvertTo(coordinate, CoordinateSystem.RDNAP); 
            
            position.xField.SetTextWithoutNotify(rdCoordinate.Points[0].ToString("0", CultureInfo.InvariantCulture));
            position.yField.SetTextWithoutNotify(rdCoordinate.Points[1].ToString("0", CultureInfo.InvariantCulture));
            position.zField.SetTextWithoutNotify(rdCoordinate.Points[2].ToString("0", CultureInfo.InvariantCulture));
        }

        private void UpdateRotationFields(Vector3 eulerAngles)
        {
            rotation.xField.SetTextWithoutNotify(eulerAngles.x.ToString("0.00", CultureInfo.InvariantCulture));
            rotation.yField.SetTextWithoutNotify(eulerAngles.y.ToString("0.00", CultureInfo.InvariantCulture));
            rotation.zField.SetTextWithoutNotify(eulerAngles.z.ToString("0.00", CultureInfo.InvariantCulture));
        }

        private void UpdateScalingFields(Vector3 localScale)
        {
            var xPercentage = localScale.x * 100;
            var yPercentage = localScale.y * 100;
            var zPercentage = localScale.z * 100;

            scale.xField.SetTextWithoutNotify($"{xPercentage.ToString("0", CultureInfo.InvariantCulture)}{percentageCharacter}");
            scale.yField.SetTextWithoutNotify($"{yPercentage.ToString("0", CultureInfo.InvariantCulture)}{percentageCharacter}");
            scale.zField.SetTextWithoutNotify($"{zPercentage.ToString("0", CultureInfo.InvariantCulture)}{percentageCharacter}");
        }
    }
}