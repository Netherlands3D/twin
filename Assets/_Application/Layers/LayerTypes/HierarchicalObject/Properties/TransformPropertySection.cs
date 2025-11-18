using System;
using System.Globalization;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties
{
    [PropertySection(typeof(TransformLayerPropertyData))]
    public class TransformPropertySection : PropertySection
    {

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

        [Header("position settings")]
        [SerializeField] private int positionDecimals = 0;
        [SerializeField] private string positionUnitCharacter = "";

        [Header("rotation settings")]
        [SerializeField] private int rotationDecimals = 2;
        [SerializeField] private string rotationUnitCharacter = "";

        [Header("scale settings")]
        [SerializeField] private float scaleMultiplier = 100f;
        [SerializeField] private int scaleDecimals = 0;

        private TransformLayerPropertyData transformPropertyData;
        private string formatString;
        
        private const string unparseableDecimalSeparator = ",";
        private const string parseableDecimalSeparator = ".";

        private void Awake()
        {
            formatString = GetFormatString(scaleDecimals);
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

            transformPropertyData.OnPositionChanged.RemoveListener(UpdatePositionFields);
            transformPropertyData.OnRotationChanged.RemoveListener(UpdateRotationFields);
            transformPropertyData.OnScaleChanged.RemoveListener(UpdateScalingFields);
        }
        
        public override void Initialize(LayerPropertyData property)
        {
            if (transformPropertyData != null)
            {
                transformPropertyData.OnPositionChanged.RemoveListener(UpdatePositionFields);
                transformPropertyData.OnRotationChanged.RemoveListener(UpdateRotationFields);
                transformPropertyData.OnScaleChanged.RemoveListener(UpdateScalingFields);
            }

            transformPropertyData = property as TransformLayerPropertyData;

            transformPropertyData.OnPositionChanged.AddListener(UpdatePositionFields);
            transformPropertyData.OnRotationChanged.AddListener(UpdateRotationFields);
            transformPropertyData.OnScaleChanged.AddListener(UpdateScalingFields);

            UpdatePositionFields(transformPropertyData.Position);
            UpdateRotationFields(transformPropertyData.EulerRotation);
            UpdateScalingFields(transformPropertyData.LocalScale);

            SetTransformLocks();
        }

        private void SetTransformLocks()
        {
                Debug.LogError("todo: move locks to property data");
            // if (layerGameObject.TryGetComponent(out TransformAxes transformLocks))
            // {
            //     position.xField.Interactable = !transformLocks.PositionLocked && transformLocks.positionAxes.HasFlag(HandleAxes.X);
            //     position.yField.Interactable = !transformLocks.PositionLocked && transformLocks.positionAxes.HasFlag(HandleAxes.Z);
            //     position.zField.Interactable = !transformLocks.PositionLocked && transformLocks.positionAxes.HasFlag(HandleAxes.Y);
            //     rotation.xField.Interactable = !transformLocks.RotationLocked && transformLocks.rotationAxes.HasFlag(HandleAxes.X);
            //     rotation.yField.Interactable = !transformLocks.RotationLocked && transformLocks.rotationAxes.HasFlag(HandleAxes.Y);
            //     rotation.zField.Interactable = !transformLocks.RotationLocked && transformLocks.rotationAxes.HasFlag(HandleAxes.Z);
            //     scale.xField.Interactable = !transformLocks.ScaleLocked && transformLocks.scaleAxes.HasFlag(HandleAxes.X);
            //     scale.yField.Interactable = !transformLocks.ScaleLocked && transformLocks.scaleAxes.HasFlag(HandleAxes.Y);
            //     scale.zField.Interactable = !transformLocks.ScaleLocked && transformLocks.scaleAxes.HasFlag(HandleAxes.Z);
            // }
            // else
            // {
            //     position.xField.Interactable = true;
            //     position.yField.Interactable = true;
            //     position.zField.Interactable = true;
            //     rotation.xField.Interactable = true;
            //     rotation.yField.Interactable = true;
            //     rotation.zField.Interactable = true;
            //     scale.xField.Interactable = true;
            //     scale.yField.Interactable = true;
            //     scale.zField.Interactable = true;
            // }
        }

        public void SetPositionX(int value)
        {
            position.xField.Text = value.ToString();
            OnPositionChanged(string.Empty);
        }

        public void SetPositionY(int value)
        {
            position.yField.Text = value.ToString();
            OnPositionChanged(string.Empty);
        }

        public void SetPositionZ(int value)
        {
            position.zField.Text = value.ToString();
            OnPositionChanged(string.Empty);
        }

        public void SetRotationX(int value)
        {
            rotation.xField.Text = value.ToString();
            OnRotationChanged(string.Empty);
        }

        public void SetRotationY(int value)
        {
            rotation.yField.Text = value.ToString();
            OnRotationChanged(string.Empty);
        }

        public void SetRotationZ(int value)
        {
            rotation.zField.Text = value.ToString();
            OnRotationChanged(string.Empty);
        }

        public void SetScaleX(int value)
        {
            scale.xField.Text = value.ToString();
            OnScaleChanged(string.Empty);
        }

        public void SetScaleY(int value)
        {
            scale.yField.Text = value.ToString();
            OnScaleChanged(string.Empty);
        }

        public void SetScaleZ(int value)
        {
            scale.zField.Text = value.ToString();
            OnScaleChanged(string.Empty);
        }

        //the param is doing nothing to match signature
        public void OnPositionChanged(string axisValue)
        {
            var numberFormat = new NumberFormatInfo
            {
                NumberDecimalSeparator = parseableDecimalSeparator
            };

            //remove the unit character and set the correct decimal separator
            var xText = position.xField.Text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var yText = position.yField.Text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var zText = position.zField.Text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
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
        public void OnRotationChanged(string axisValue)
        {
            var numberFormat = new NumberFormatInfo
            {
                NumberDecimalSeparator = parseableDecimalSeparator
            };

            //remove the unit character and set the correct decimal separator
            var xText = rotation.xField.Text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var yText = rotation.yField.Text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var zText = rotation.zField.Text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);

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
        public void OnScaleChanged(string axisValue)
        {
            var numberFormat = new NumberFormatInfo
            {
                NumberDecimalSeparator = parseableDecimalSeparator
            };

            //remove the unit character and set the correct decimal separator
            var xText = scale.xField.Text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var yText = scale.yField.Text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);
            var zText = scale.zField.Text.Replace(unparseableDecimalSeparator, parseableDecimalSeparator);            

            if (transformPropertyData.ScaleUnitCharacter.Length > 0)
            {
                xText = xText.Replace(transformPropertyData.ScaleUnitCharacter, string.Empty);
                yText = yText.Replace(transformPropertyData.ScaleUnitCharacter, string.Empty);
                zText = zText.Replace(transformPropertyData.ScaleUnitCharacter, string.Empty);
            }
            
            float.TryParse(xText, NumberStyles.Float, numberFormat, out var x);
            float.TryParse(yText, NumberStyles.Float, numberFormat, out var y);
            float.TryParse(zText, NumberStyles.Float, numberFormat, out var z);

            transformPropertyData.LocalScale = new Vector3(x / scaleMultiplier, y / scaleMultiplier, z / scaleMultiplier);
        }

        private void UpdatePositionFields(Coordinate coordinate)
        {
            var rdCoordinate = coordinate.Convert( CoordinateSystem.RDNAP);
            
            position.xField.SetTextWithoutNotify($"{rdCoordinate.easting.ToString(formatString, CultureInfo.InvariantCulture)}{positionUnitCharacter}");
            position.yField.SetTextWithoutNotify($"{rdCoordinate.northing.ToString(formatString, CultureInfo.InvariantCulture)}{positionUnitCharacter}");
            position.zField.SetTextWithoutNotify($"{rdCoordinate.height.ToString(formatString, CultureInfo.InvariantCulture)}{positionUnitCharacter}");
        }

        private void UpdateRotationFields(Vector3 eulerAngles)
        {
            rotation.xField.SetTextWithoutNotify($"{eulerAngles.x.ToString(formatString, CultureInfo.InvariantCulture)}{rotationUnitCharacter}");
            rotation.yField.SetTextWithoutNotify($"{eulerAngles.y.ToString(formatString, CultureInfo.InvariantCulture)}{rotationUnitCharacter}");
            rotation.zField.SetTextWithoutNotify($"{eulerAngles.z.ToString(formatString, CultureInfo.InvariantCulture)}{rotationUnitCharacter}");
        }

        private void UpdateScalingFields(Vector3 localScale)
        {
            var xPercentage = localScale.x * scaleMultiplier;
            var yPercentage = localScale.y * scaleMultiplier;
            var zPercentage = localScale.z * scaleMultiplier;

            scale.xField.SetTextWithoutNotify($"{xPercentage.ToString(formatString, CultureInfo.InvariantCulture)}{transformPropertyData.ScaleUnitCharacter}");
            scale.yField.SetTextWithoutNotify($"{yPercentage.ToString(formatString, CultureInfo.InvariantCulture)}{transformPropertyData.ScaleUnitCharacter}");
            scale.zField.SetTextWithoutNotify($"{zPercentage.ToString(formatString, CultureInfo.InvariantCulture)}{transformPropertyData.ScaleUnitCharacter}");
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