using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class MaskLayerListViewItem : VisualElement, IVisualizationWithPropertyData
    {
        private int maskingBitIndex;
        
        private VisibilityToggle MaskActiveToggle => this.Q<VisibilityToggle>("MaskActiveToggle");
        private Label LayerNameLabel => this.Q<Label>("LayerNameLabel");
        
        private Icon layerTypeIcon;
        private Icon LayerTypeIcon => layerTypeIcon ??= this.Q<Icon>("LayerTypeIcon");
        
        private LayerData layerData => userData as LayerData;

        public MaskLayerListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            MaskActiveToggle.RegisterValueChangedCallback(OnToggleChanged);
        }
        
        private void OnToggleChanged(ChangeEvent<bool> evt)
        {
            SetMaskingBit(evt.newValue);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var maskingLayerPropertyData = properties.Get<MaskingLayerPropertyData>();
            var isMaskable = maskingLayerPropertyData != null;
            
            if (isMaskable)
            {
                var isOn = GetIsMaskingBitSet(maskingLayerPropertyData);
                MaskActiveToggle.value = isOn;
            }
            else
            {
                MaskActiveToggle.SetEnabled(false); 
            }
        }

        public void Initialize(LayerData layerData, int maskingBitIndex)
        {
            this.maskingBitIndex = maskingBitIndex;
            userData = layerData;
            LayerNameLabel.text = layerData.Name;
            LoadProperties(layerData.LayerProperties);
            LayerTypeIcon.Image = GetImage(layerData);
        }

        private static IconImage GetImage(LayerData layerData)
        {
            return LayerTypeSpriteLibrary.GetIconImage(layerData);
        }

        private bool GetIsMaskingBitSet(MaskingLayerPropertyData layerPropertyData)
        {
            var currentLayerMask = layerPropertyData.GetMaskLayerMask();
            int maskBitToCheck = 1 << maskingBitIndex;
            bool isBitSet = (currentLayerMask & maskBitToCheck) != 0;
            return isBitSet;
        }

        private void SetMaskingBit(bool active)
        {
            MaskingLayerPropertyData propertyData = layerData.GetProperty<MaskingLayerPropertyData>();
            propertyData.SetMaskBit(maskingBitIndex, active);
        }
    }
}