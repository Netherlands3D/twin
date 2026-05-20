using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.Events;
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
        
        public UnityEvent<bool> VisibilityToggleChanged = new UnityEvent<bool>();

        public MaskLayerListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            MaskActiveToggle.RegisterValueChangedCallback(OnToggleChanged);
        }

        private void OnToggleChanged(ChangeEvent<bool> evt)
        {
            VisibilityToggleChanged.Invoke(evt.newValue); //invoke an event instead of setting the bit directly, because we need to account for multi-selected items
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            MaskActiveToggle.UnregisterValueChangedCallback(OnToggleChanged);
            var maskingLayerPropertyData = properties.Get<MaskingLayerPropertyData>();
            var isMaskable = maskingLayerPropertyData != null;

            if (isMaskable)
            {
                var isOn = GetIsMaskingBitSet(maskingLayerPropertyData);
                MaskActiveToggle.value = isOn;
            }
            else
            {
                UpdateToggleFromChildren();
            }
            MaskActiveToggle.RegisterValueChangedCallback(OnToggleChanged);
            
            MaskActiveToggle.EnableInClassList(UtilityClassConstants.HIDDEN, !isMaskable);
        }

        private void UpdateToggleFromChildren()
        {
            if (layerData.ChildrenLayers.Count == 0)
                return;

            bool anyOn = false;
            bool anyOff = false;

            foreach (var child in layerData.ChildrenLayers)
            {
                var childMasking = child.GetProperty<MaskingLayerPropertyData>();
                if (childMasking == null) continue;

                bool isOn = GetIsMaskingBitSet(childMasking);
                if (isOn)
                    anyOn = true;
                else
                    anyOff = true;
            }

            if (anyOn && anyOff)
                MaskActiveToggle.SetState(VisibilityState.PartiallyVisible);
            else if (anyOn)
                MaskActiveToggle.SetState(VisibilityState.Visible);
            else
                MaskActiveToggle.SetState(VisibilityState.Invisible);
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
    }
}