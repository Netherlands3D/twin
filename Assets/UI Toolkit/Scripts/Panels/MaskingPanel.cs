using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;
using TreeView = UnityEngine.UIElements.TreeView;


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
        }
        
        public MaskingPanel(LayerData rootLayer, int maskBitIndex) : this()
        {
            this.maskBitIndex = maskBitIndex;
            treeView = this.Q<TreeView>();
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
            var tree = rootLayer.ToTreeViewItems(IsMaskableOrHasChildren);
            treeView.SetRootItems(tree);
            treeView.RefreshItems();
        }

        private bool IsMaskableOrHasChildren(LayerData layer)
        {
            return layer.ChildrenLayers.Any(IsMaskableOrHasChildren) || layer.GetProperty<MaskingLayerPropertyData>() != null;
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