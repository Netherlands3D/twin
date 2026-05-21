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
            }).Every(0);  // 0ms = runs every frame
        }

        public MaskingPanel(LayerData rootLayer, int maskBitIndex) : this()
        {
            this.maskBitIndex = maskBitIndex;
            treeView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            treeView.selectionType = SelectionType.Multiple;

            treeView.makeItem = MakeItem;
            treeView.bindItem = BindItem;

            PopulateMaskLayerPanel(rootLayer);
        }

        private VisualElement MakeItem()
        {
            return new MaskLayerListViewItem();
        }

        private void BindItem(VisualElement item, int index)
        {
            if (item is not MaskLayerListViewItem maskLayerRowElement) return;

            maskLayerRowElement.VisibilityToggleChanged.RemoveListener(ToggleSelection);
            var layerData = treeView.GetItemDataForIndex<LayerData>(index);
            maskLayerRowElement.Initialize(index, layerData, maskBitIndex);
            maskLayerRowElement.VisibilityToggleChanged.AddListener(ToggleSelection);
        }

        private void ToggleSelection(int clickedIndex, bool active)
        {
            var selectedIndices = treeView.selectedIndices.ToList();
            if(!selectedIndices.Contains(clickedIndex)) //we toggled a different layer than the selected layers, don't toggle the selected layers
                return;
            
            foreach (var index in selectedIndices) //make a copy of the indices, because they might change
            {
                var layerData = treeView.GetItemDataForIndex<LayerData>(index);
                MaskingLayerPropertyData propertyData = layerData.GetProperty<MaskingLayerPropertyData>();
                propertyData.SetMaskBit(maskBitIndex, active);
            }

            RefreshAtEndOfFrame();
        }

        private void PopulateMaskLayerPanel(LayerData rootLayer)
        {
            var tree = ToTreeViewItems(rootLayer);
            treeView.SetRootItems(tree);
            RefreshAtEndOfFrame();
        }

        private List<TreeViewItemData<LayerData>> ToTreeViewItems(LayerData rootLayer)
        {
            var counter = 0;
            return BuildRecursive(rootLayer.ChildrenLayers, ref counter);
        }

        private List<TreeViewItemData<LayerData>> BuildRecursive(List<LayerData> layers, ref int counter)
        {
            var result = new List<TreeViewItemData<LayerData>>();
            if (layers == null) return result;

            foreach (var layer in layers)
            {
                var children = BuildRecursive(layer.ChildrenLayers, ref counter);
                var maskingPropertyData = layer.GetProperty<MaskingLayerPropertyData>();
                bool isMaskable = maskingPropertyData != null;
                if (isMaskable)
                {
                    //when masking changes, refresh the panel so all the toggles get the correct visibility state.
                    //We do not call RefreshItems directly because when a multiselect toggles multiple items, we need only 1 refresh
                    maskingPropertyData.OnStylingChanged.AddListener(RefreshAtEndOfFrame); 
                }

                if (isMaskable || children.Count > 0)
                    result.Add(new TreeViewItemData<LayerData>(
                        counter++,
                        layer,
                        children.Count > 0 ? children : null
                    ));
            }

            return result;
        }

        private void RefreshAtEndOfFrame()
        {
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