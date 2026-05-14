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
    public partial class LayerListViewItem : VisualElement, IVisualizationWithPropertyData
    {
        private VisibilityToggle isActiveToggle;
        private Icon layerTypeIcon;
        private Label layerNameLabel;
        
        private LayerData layerData => userData as LayerData;

        public LayerListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            isActiveToggle = this.Q<VisibilityToggle>("IsActiveToggle");
            layerTypeIcon = this.Q<Icon>("LayerTypeIcon");
            layerNameLabel = this.Q<Label>("LayerNameLabel");
            
            isActiveToggle.RegisterValueChangedCallback(OnIsActiveToggleChanged);
        }

        private void OnIsActiveToggleChanged(ChangeEvent<bool> evt)
        {
            layerData.ActiveSelf = evt.newValue;
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            // var maskingLayerPropertyData = properties.Get<MaskingLayerPropertyData>();
            // var isMaskable = maskingLayerPropertyData != null;
            //
            // if (isMaskable)
            // {
            //     var isOn = GetIsMaskingBitSet(maskingLayerPropertyData);
            //     isActiveToggle.value = isOn;
            // }
            // else
            // {
            //     UpdateToggleFromChildren();
            // }
            //
            // isActiveToggle.EnableInClassList(UtilityClassConstants.HIDDEN, !isMaskable);
        }

        // private void UpdateToggleFromChildren()
        // {
        //     if (layerData.ChildrenLayers.Count == 0)
        //         return;
        //
        //     bool anyOn = false;
        //     bool anyOff = false;
        //
        //     foreach (var child in layerData.ChildrenLayers)
        //     {
        //         var childMasking = child.GetProperty<MaskingLayerPropertyData>();
        //         if (childMasking == null) continue;
        //
        //         bool isOn = GetIsMaskingBitSet(childMasking);
        //         if (isOn)
        //             anyOn = true;
        //         else
        //             anyOff = true;
        //     }
        //
        //     if (anyOn && anyOff)
        //         isActiveToggle.SetState(VisibilityState.PartiallyVisible);
        //     else if (anyOn)
        //         isActiveToggle.SetState(VisibilityState.Visible);
        //     else
        //         isActiveToggle.SetState(VisibilityState.Invisible);
        // }

        public void Initialize(LayerData layerData)
        {
            userData = layerData;
            layerNameLabel.text = layerData.Name;
            LoadProperties(layerData.LayerProperties);
            layerTypeIcon.Image = GetImage(layerData);
        }

        private static IconImage GetImage(LayerData layerData)
        {
            return LayerTypeSpriteLibrary.GetIconImage(layerData);
        }
    }
}