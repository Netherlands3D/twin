using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class MaskLayerListViewItem : VisualElement, IVisualizationWithPropertyData
    {
        private Toggle MaskActiveToggle => this.Q<Toggle>("MaskActiveToggle"); //todo: this is now wrapped in a visual element for layout, should this be a component?
        private Label LayerNameLabel => this.Q<Label>("LayerNameLabel"); //todo: this is now wrapped in a visual element for layout, should this be a component?

        private LayerData layerData => userData as LayerData;

        public MaskLayerListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            MaskActiveToggle.RegisterValueChangedCallback(OnToggleChanged);
        }
        
        private void OnToggleChanged(ChangeEvent<bool> evt)
        {
            SetDomeMaskingBit(evt.newValue);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var layerPropertyData = layerData.GetProperty<MaskingLayerPropertyData>();
            var isOn = GetIsDomeMaskingBitSet(layerPropertyData);
            MaskActiveToggle.SetValueWithoutNotify(isOn);
        }

        public void Initialize(LayerData layerData)
        {
            userData = layerData;
            LayerNameLabel.text = layerData.Name;
            LoadProperties(layerData.LayerProperties);
        }
        
        private bool GetIsDomeMaskingBitSet(MaskingLayerPropertyData layerPropertyData)
        {
            var currentLayerMask = layerPropertyData.GetMaskLayerMask();
            int maskBitToCheck = 1 << MaskingLayerPropertyData.MASKING_DOME_BIT_INDEX;
            bool isBitSet = (currentLayerMask & maskBitToCheck) != 0;
            return isBitSet;
        }

        private void SetDomeMaskingBit(bool active)
        {
            MaskingLayerPropertyData propertyData = layerData.GetProperty<MaskingLayerPropertyData>();
            propertyData.SetMaskBit(MaskingLayerPropertyData.MASKING_DOME_BIT_INDEX, active);
        }
    }
}