using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;


namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DomePanel : VisualElement
    {
        private ListView listView;
        private ListView ListView => listView ??= this.Q<ListView>();

        public DomePanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
        }
        
        public DomePanel(Dictionary<string, object> data) : this()
        {
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;
        
            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;
            
            PopulateMaskLayerPanel(data);
        }
        
        private VisualElement MakeListViewItem()
        {
            return new MaskLayerListViewItem();
        }

        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not MaskLayerListViewItem maskLayerRowElement) return;
            
            var layerData = ListView.itemsSource[index] as LayerData;
            maskLayerRowElement.Initialize(layerData);
        }
        
        private void PopulateMaskLayerPanel(Dictionary<string, object> data)
        {
            ListView.itemsSource = data.Values.ToList();
            ListView.RefreshItems();
        }
    }
}