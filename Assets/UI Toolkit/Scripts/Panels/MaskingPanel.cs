using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = Netherlands3D.UI.Components.TreeView;


namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class MaskingPanel : VisualElement
    {
        private int maskBitIndex;
        private TreeView treeView;
        private bool refreshAtEndOfFrame;

        public MaskingPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            treeView = this.Q<TreeView>();

            schedule.Execute(() =>
            {
                if (!refreshAtEndOfFrame) return;

                refreshAtEndOfFrame = false;
                treeView.RefreshItems();
            }).Every(0); // 0ms = runs every frame
        }

        public MaskingPanel(LayerData rootLayer, int maskBitIndex) : this()
        {
            this.maskBitIndex = maskBitIndex;
            treeView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            treeView.selectionType = SelectionType.Multiple;

            treeView.makeItem = MakeItem;
            treeView.bindItem = BindItem;
            treeView.unbindItem = UnbindItem;

            var tree = rootLayer.ToTreeViewItems(IsMaskable, false);
            treeView.SetRootItems(tree);
            RefreshAtEndOfFrame();
        }

        private VisualElement MakeItem()
        {
            return new MaskLayerListViewItem();
        }

        private void BindItem(VisualElement item, int index)
        {
            if (item is not MaskLayerListViewItem maskLayerRowElement) return;

            var layerData = treeView.GetItemDataForIndex<LayerData>(index);
            maskLayerRowElement.Initialize(index, layerData, maskBitIndex);
            
            maskLayerRowElement.VisibilityToggleChanged.AddListener(ToggleSelection);

            var maskingLayerPropertyData = layerData.GetProperty<MaskingLayerPropertyData>();
            if (maskingLayerPropertyData != null)
            {
                maskingLayerPropertyData.OnStylingChanged.AddListener(RefreshAtEndOfFrame);
            }
        }
        
        private void UnbindItem(VisualElement item, int index)
        {
            if (item is not MaskLayerListViewItem maskLayerRowElement) return;
            
            var layerData = item.userData as LayerData;
            
            maskLayerRowElement.VisibilityToggleChanged.RemoveListener(ToggleSelection);

            var maskingLayerPropertyData = layerData.GetProperty<MaskingLayerPropertyData>();
            if (maskingLayerPropertyData != null)
            {
                maskingLayerPropertyData.OnStylingChanged.RemoveListener(RefreshAtEndOfFrame);
            }
        }

        private void ToggleSelection(int clickedIndex, bool active)
        {
            var selectedIndices = treeView.selectedIndices.ToList();
            if (!selectedIndices.Contains(clickedIndex)) //we toggled a different layer than the selected layers, don't toggle the selected layers
                return;

            foreach (var index in selectedIndices) //make a copy of the indices, because they might change
            {
                var layerData = treeView.GetItemDataForIndex<LayerData>(index);
                MaskingLayerPropertyData propertyData = layerData.GetProperty<MaskingLayerPropertyData>();
                propertyData.SetMaskBit(maskBitIndex, active);
            }

            RefreshAtEndOfFrame();
        }

        private bool IsMaskable(LayerData layer)
        {
            var maskingPropertyData = layer.GetProperty<MaskingLayerPropertyData>();
            return maskingPropertyData != null;
        }

        private void RefreshAtEndOfFrame()
        {
            Debug.Log("RefreshAtEndOfFrame");
            refreshAtEndOfFrame = true;
        }

        public void SetHeader(string headerText)
        {
            this.Q<Header>().LabelText = headerText;
        }

        public void SetDescription(string description)
        {
            this.Q<Label>("Description").text = description;
        }
    }
}