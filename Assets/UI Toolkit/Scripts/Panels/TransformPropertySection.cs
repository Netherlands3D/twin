using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using RuntimeHandle;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(TransformLayerPropertyData))]
    public partial class TransformPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private class SetOfXYZ
        {
            public ValueField xField;
            public ValueField yField;
            public ValueField zField;

            public SetOfXYZ(ValueField xField, ValueField yField, ValueField zField)
            {
                this.xField = xField;
                this.yField = yField;
                this.zField = zField;
            }

            public void EnableAxes(HandleAxes enabledAxes)
            {
                xField.SetEnabled(enabledAxes.HasFlag(HandleAxes.X));
                yField.SetEnabled(enabledAxes.HasFlag(HandleAxes.Y));
                zField.SetEnabled(enabledAxes.HasFlag(HandleAxes.Z));
            }
        }

        private SetOfXYZ position;
        private SetOfXYZ rotation;
        private SetOfXYZ scale;
        
        private TransformLayerPropertyData transformPropertyData;
        
        public TransformPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
            
            position = new(this.Q<ValueField>("PositionX"), this.Q<ValueField>("PositionY"), this.Q<ValueField>("PositionZ"));
            rotation = new(this.Q<ValueField>("RotationX"), this.Q<ValueField>("RotationY"), this.Q<ValueField>("RotationZ"));
            scale = new(this.Q<ValueField>("ScaleX"), this.Q<ValueField>("ScaleY"), this.Q<ValueField>("ScaleZ"));
            
            position.xField.InputField.RegisterValueChangedCallback(OnPositionChanged);
            position.yField.InputField.RegisterValueChangedCallback(OnPositionChanged);
            position.zField.InputField.RegisterValueChangedCallback(OnPositionChanged);
            rotation.xField.InputField.RegisterValueChangedCallback(OnRotationChanged);
            rotation.yField.InputField.RegisterValueChangedCallback(OnRotationChanged);
            rotation.zField.InputField.RegisterValueChangedCallback(OnRotationChanged);
            scale.xField.InputField.RegisterValueChangedCallback(OnScaleChanged);
            scale.yField.InputField.RegisterValueChangedCallback(OnScaleChanged);
            scale.zField.InputField.RegisterValueChangedCallback(OnScaleChanged);
        }

        //the param is doing nothing to match signature
        private void OnPositionChanged(ChangeEvent<string> onChange)
        {
            var x = position.xField.GetValueAsDouble();
            var y = position.yField.GetValueAsDouble();
            var z = position.zField.GetValueAsDouble();

            var rdCoordinate = new Coordinate(CoordinateSystem.RDNAP, x, y, z);
            transformPropertyData.Position = rdCoordinate;
        }
        
        //the param is doing nothing to match signature
        private void OnRotationChanged(ChangeEvent<string> onChange)
        {
            var x = rotation.xField.GetValueAsDouble();
            var y = rotation.yField.GetValueAsDouble();
            var z = rotation.zField.GetValueAsDouble();

            transformPropertyData.EulerRotation = new Vector3((float)x, (float)y, (float)z);
        }
        
        //the param is doing nothing to match signature
        private void OnScaleChanged(ChangeEvent<string> onChange)
        {
            var x = scale.xField.GetValueAsDouble();
            var y = scale.yField.GetValueAsDouble();
            var z = scale.zField.GetValueAsDouble();

            float scaleMultiplier = transformPropertyData.ScaleUnitCharacter == "%" ? 100f : 1f;
            transformPropertyData.LocalScale = new Vector3((float)x / scaleMultiplier, (float)y / scaleMultiplier, (float)z / scaleMultiplier);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            transformPropertyData = properties.Get<TransformLayerPropertyData>();

            transformPropertyData.OnPositionChanged.AddListener(UpdatePositionFields);
            transformPropertyData.OnRotationChanged.AddListener(UpdateRotationFields);
            transformPropertyData.OnScaleChanged.AddListener(UpdateScalingFields);
            
            SetUnitCharacter(scale, transformPropertyData.ScaleUnitCharacter);
            
            UpdatePositionFields(transformPropertyData.Position);
            UpdateRotationFields(transformPropertyData.EulerRotation);
            UpdateScalingFields(transformPropertyData.LocalScale);

            SetTransformLocks(properties);
        }

        private void SetUnitCharacter(SetOfXYZ fields, string unitCharacter)
        {
            fields.xField.UnitCharacter = unitCharacter;
            fields.yField.UnitCharacter = unitCharacter;
            fields.zField.UnitCharacter = unitCharacter;
        }

        private void SetTransformLocks(List<LayerPropertyData> properties)
        {
            TransformLockLayerPropertyData propertyData = properties.Get<TransformLockLayerPropertyData>();
            if (propertyData == null)
            {
                position.EnableAxes(HandleAxes.XYZ);
                rotation.EnableAxes(HandleAxes.XYZ);
                scale.EnableAxes(HandleAxes.XYZ);
                return;
            }
            
            HandleAxes enabledPositionAxes = (HandleAxes)propertyData.PositionAxes;
            HandleAxes enabledRotationAxes = (HandleAxes)propertyData.RotationAxes;
            HandleAxes enabledScaleAxes = (HandleAxes)propertyData.ScaleAxes;
            
            position.EnableAxes(enabledPositionAxes);
            rotation.EnableAxes(enabledRotationAxes);
            scale.EnableAxes(enabledScaleAxes);
        }
        
        private void UpdatePositionFields(Coordinate newPosition)
        {
            var rdCoordinate = newPosition.Convert(CoordinateSystem.RDNAP);
            position.xField.SetValueWithoutNotify(rdCoordinate.easting);
            position.yField.SetValueWithoutNotify(rdCoordinate.northing);
            position.zField.SetValueWithoutNotify(rdCoordinate.height);
        }

        private void UpdateRotationFields(Vector3 eulerAngles)
        {
            rotation.xField.SetValueWithoutNotify(eulerAngles.x);
            rotation.yField.SetValueWithoutNotify(eulerAngles.y);
            rotation.zField.SetValueWithoutNotify(eulerAngles.z);
        }

        private void UpdateScalingFields(Vector3 newScale)
        {
            float scaleMultiplier = transformPropertyData.ScaleUnitCharacter == "%" ? 100f : 1f;
            var x = newScale.x * scaleMultiplier;
            var y = newScale.y * scaleMultiplier;
            var z = newScale.z * scaleMultiplier;

            scale.xField.SetValueWithoutNotify(x);
            scale.yField.SetValueWithoutNotify(y);
            scale.zField.SetValueWithoutNotify(z);
        }
    }
}