using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI_Toolkit.Scripts.Panels;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using TreeView = UnityEngine.UIElements.TreeView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class LayerPanel : BaseInspectorContentPanel
    {
        private TreeView treeView;

        public LayerPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            treeView = this.Q<TreeView>();
            
            treeView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            treeView.selectionType = SelectionType.Multiple;
        
            treeView.makeItem = MakeItem;
            treeView.bindItem = BindItem;
        }

        public void PopulateLayerPanel(LayerData rootLayer)
        {
            var tree = rootLayer.ToTreeViewItems();
            treeView.SetRootItems(tree);
            treeView.RefreshItems();
        }

        private VisualElement MakeItem()
        {
            return new LayerListViewItem();
        }

        private void BindItem(VisualElement item, int index)
        {
            if (item is not LayerListViewItem layerRowElement) return;
    
            var layerData = treeView.GetItemDataForIndex<LayerData>(index);
            layerRowElement.Initialize(layerData);
        }
        
        public override string GetTitle()
        {
            return "Lagen";
        }
    }
}