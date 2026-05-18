using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;
using TreeView = Netherlands3D.UI.Components.TreeView;


namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class MaskingPanel : VisualElement
    {
        private int maskBitIndex;
        private TreeView treeView;

        public MaskingPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
            treeView = this.Q<TreeView>();
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
    
            var layerData = treeView.GetItemDataForIndex<LayerData>(index);
            maskLayerRowElement.Initialize(layerData, maskBitIndex);
        }
        
        private void PopulateMaskLayerPanel(LayerData rootLayer)
        {
            var tree = ToTreeViewItems(rootLayer);
            treeView.SetRootItems(tree);
            treeView.RefreshItems();
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
                    maskingPropertyData.OnStylingChanged.AddListener(treeView.RefreshItems); //when masking changes, refresh the panel so all the toggles get the correct visibility state
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