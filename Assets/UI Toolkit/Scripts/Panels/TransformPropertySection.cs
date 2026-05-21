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
    [PropertySection(typeof(TransformLayerPropertyData), PropertySectionCategory.Settings)]
    public partial class TransformPropertySection : VisualElement, IVisualizationWithPropertyData
    {
        private XYZField position;
        private XYZField rotation;
        private XYZField scale;

        private TransformLayerPropertyData transformPropertyData;

        public TransformPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            position = this.Q<XYZField>("Position");
            rotation = this.Q<XYZField>("Rotation");
            scale = this.Q<XYZField>("Scale");

            position.xField.InputField.RegisterCallback<BlurEvent>(_ => OnPositionChanged());
            position.yField.InputField.RegisterCallback<BlurEvent>(_ => OnPositionChanged());
            position.zField.InputField.RegisterCallback<BlurEvent>(_ => OnPositionChanged());
            rotation.xField.InputField.RegisterCallback<BlurEvent>(_ => OnRotationChanged());
            rotation.yField.InputField.RegisterCallback<BlurEvent>(_ => OnRotationChanged());
            rotation.zField.InputField.RegisterCallback<BlurEvent>(_ => OnRotationChanged());
            scale.xField.InputField.RegisterCallback<BlurEvent>(_ => OnScaleChanged());
            scale.yField.InputField.RegisterCallback<BlurEvent>(_ => OnScaleChanged());
            scale.zField.InputField.RegisterCallback<BlurEvent>(_ => OnScaleChanged());
            
            position.xField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => position.xField.Focus(), TrickleDown.TrickleDown);
            position.yField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => position.yField.Focus(), TrickleDown.TrickleDown);
            position.zField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => position.zField.Focus(), TrickleDown.TrickleDown);
            rotation.xField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => rotation.xField.Focus(), TrickleDown.TrickleDown);
            rotation.yField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => rotation.yField.Focus(), TrickleDown.TrickleDown);
            rotation.zField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => rotation.zField.Focus(), TrickleDown.TrickleDown);
            scale.xField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => scale.xField.Focus(), TrickleDown.TrickleDown);
            scale.yField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => scale.yField.Focus(), TrickleDown.TrickleDown);
            scale.zField.InputField.RegisterCallback<NavigationSubmitEvent>(_ => scale.zField.Focus(), TrickleDown.TrickleDown);
        }
        
        private void OnPositionChanged()
        {
            var x = position.xField.GetValueAsDouble();
            var y = position.yField.GetValueAsDouble();
            var z = position.zField.GetValueAsDouble();

            var rdCoordinate = new Coordinate(CoordinateSystem.RDNAP, x, y, z);
            transformPropertyData.Position = rdCoordinate;
        }

        private void OnRotationChanged()
        {
            var x = rotation.xField.GetValueAsDouble();
            var y = rotation.yField.GetValueAsDouble();
            var z = rotation.zField.GetValueAsDouble();

            transformPropertyData.EulerRotation = new Vector3((float)x, (float)y, (float)z);
        }

        private void OnScaleChanged()
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

        private void SetUnitCharacter(XYZField fields, string unitCharacter)
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