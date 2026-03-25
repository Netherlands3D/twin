using System.Collections.Generic;
using System.Globalization;
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
    public partial class TransformPanel : VisualElement, IVisualizationWithPropertyData
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

        private string positionUnitCharacter = "";
        private string rotationUnitCharacter = "";
        private string formatString;
        
        private const string unparseableDecimalSeparator = ",";
        private const string parseableDecimalSeparator = ".";
        
        public TransformPanel()
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
            
            formatString = GetFormatString(0);
        }

        //the param is doing nothing to match signature
        private void OnPositionChanged(ChangeEvent<string> onChange)
        {
            var numberFormat = new NumberFormatInfo
            {
                NumberDecimalSeparator = parseableDecimalSeparator
            };

            //remove the unit character and set the correct decimal separator
            var xText = position.xField.InputField.text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var yText = position.yField.InputField.text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var zText = position.zField.InputField.text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            if (positionUnitCharacter.Length > 0)
            {
                xText = xText.Replace(positionUnitCharacter, string.Empty);
                yText = yText.Replace(positionUnitCharacter, string.Empty);
                zText = zText.Replace(positionUnitCharacter, string.Empty);
            }
            
            double.TryParse(xText, NumberStyles.Float, numberFormat, out var x);
            double.TryParse(yText, NumberStyles.Float, numberFormat, out var y);
            double.TryParse(zText, NumberStyles.Float, numberFormat, out var z);

            var rdCoordinate = new Coordinate(CoordinateSystem.RDNAP, x, y, z);
            transformPropertyData.Position = rdCoordinate;
        }
        
        //the param is doing nothing to match signature
        private void OnRotationChanged(ChangeEvent<string> onChange)
        {
            var numberFormat = new NumberFormatInfo
            {
                NumberDecimalSeparator = parseableDecimalSeparator
            };

            //remove the unit character and set the correct decimal separator
            var xText = rotation.xField.InputField.text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var yText = rotation.yField.InputField.text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var zText = rotation.zField.InputField.text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);

            if (rotationUnitCharacter.Length > 0)
            {
                xText = xText.Replace(rotationUnitCharacter, string.Empty);
                yText = yText.Replace(rotationUnitCharacter, string.Empty);
                zText = zText.Replace(rotationUnitCharacter, string.Empty);
            }
            
            float.TryParse(xText, NumberStyles.Float, numberFormat, out var x);
            float.TryParse(yText, NumberStyles.Float, numberFormat, out var y);
            float.TryParse(zText, NumberStyles.Float, numberFormat, out var z);

            transformPropertyData.EulerRotation = new Vector3(x, y, z);
        }
        
        //the param is doing nothing to match signature
        private void OnScaleChanged(ChangeEvent<string> onChange)
        {
            var numberFormat = new NumberFormatInfo
            {
                NumberDecimalSeparator = parseableDecimalSeparator
            };

            //remove the unit character and set the correct decimal separator
            var xText = scale.xField.InputField.text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var yText = scale.yField.InputField.text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var zText = scale.zField.InputField.text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);            

            if (transformPropertyData.ScaleUnitCharacter.Length > 0)
            {
                xText = xText.Replace(transformPropertyData.ScaleUnitCharacter, string.Empty);
                yText = yText.Replace(transformPropertyData.ScaleUnitCharacter, string.Empty);
                zText = zText.Replace(transformPropertyData.ScaleUnitCharacter, string.Empty);
            }
            
            float.TryParse(xText, NumberStyles.Float, numberFormat, out var x);
            float.TryParse(yText, NumberStyles.Float, numberFormat, out var y);
            float.TryParse(zText, NumberStyles.Float, numberFormat, out var z);

            float scaleMultiplier = transformPropertyData.ScaleUnitCharacter == "%" ? 100f : 1f;
            transformPropertyData.LocalScale = new Vector3(x / scaleMultiplier, y / scaleMultiplier, z / scaleMultiplier);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            transformPropertyData = properties.Get<TransformLayerPropertyData>();

            transformPropertyData.OnPositionChanged.AddListener(UpdatePositionFields);
            transformPropertyData.OnRotationChanged.AddListener(UpdateRotationFields);
            transformPropertyData.OnScaleChanged.AddListener(UpdateScalingFields);
            
            UpdatePositionFields(transformPropertyData.Position);
            UpdateRotationFields(transformPropertyData.EulerRotation);
            UpdateScalingFields(transformPropertyData.LocalScale);

            SetTransformLocks(properties);
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
            var rdCoordinate = newPosition.Convert( CoordinateSystem.RDNAP);

            position.xField.InputField.SetValueWithoutNotify($"{rdCoordinate.easting.ToString(formatString, CultureInfo.InvariantCulture)}{positionUnitCharacter}");
            position.yField.InputField.SetValueWithoutNotify($"{rdCoordinate.northing.ToString(formatString, CultureInfo.InvariantCulture)}{positionUnitCharacter}");
            position.zField.InputField.SetValueWithoutNotify($"{rdCoordinate.height.ToString(formatString, CultureInfo.InvariantCulture)}{positionUnitCharacter}");
        }

        private void UpdateRotationFields(Vector3 eulerAngles)
        {
            rotation.xField.InputField.SetValueWithoutNotify($"{eulerAngles.x.ToString(formatString, CultureInfo.InvariantCulture)}{rotationUnitCharacter}");
            rotation.yField.InputField.SetValueWithoutNotify($"{eulerAngles.y.ToString(formatString, CultureInfo.InvariantCulture)}{rotationUnitCharacter}");
            rotation.zField.InputField.SetValueWithoutNotify($"{eulerAngles.z.ToString(formatString, CultureInfo.InvariantCulture)}{rotationUnitCharacter}");
        }

        private void UpdateScalingFields(Vector3 newScale)
        {
            float scaleMultiplier = transformPropertyData.ScaleUnitCharacter == "%" ? 100f : 1f;
            var xPercentage = newScale.x * scaleMultiplier;
            var yPercentage = newScale.y * scaleMultiplier;
            var zPercentage = newScale.z * scaleMultiplier;

            scale.xField.InputField.SetValueWithoutNotify($"{xPercentage.ToString(formatString, CultureInfo.InvariantCulture)}{transformPropertyData.ScaleUnitCharacter}");
            scale.yField.InputField.SetValueWithoutNotify($"{yPercentage.ToString(formatString, CultureInfo.InvariantCulture)}{transformPropertyData.ScaleUnitCharacter}");
            scale.zField.InputField.SetValueWithoutNotify($"{zPercentage.ToString(formatString, CultureInfo.InvariantCulture)}{transformPropertyData.ScaleUnitCharacter}");
        }
        
        private static string GetFormatString(int decimals)
        {
            if (decimals == 0)
                return "0";

            string zeros = new string('0', decimals);
            return $"0.{zeros}";
        }
    }
}