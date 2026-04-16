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
    public partial class MaskingPanel : VisualElement
    {
        private int maskBitIndex;
        
        private ListView listView;
        private ListView ListView => listView ??= this.Q<ListView>();

        public MaskingPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");    
        }
        
        public MaskingPanel(List<LayerData> layers, int maskBitIndex) : this()
        {
            this.maskBitIndex = maskBitIndex;
            
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;
        
            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;
            
            PopulateMaskLayerPanel(layers);
        }
        
        private VisualElement MakeListViewItem()
        {
            return new MaskLayerListViewItem();
        }

        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not MaskLayerListViewItem maskLayerRowElement) return;
            
            var layerData = ListView.itemsSource[index] as LayerData;
            maskLayerRowElement.Initialize(layerData, maskBitIndex);
        }
        
        private void PopulateMaskLayerPanel(List<LayerData> layers)
        {
            ListView.itemsSource = layers;
            ListView.RefreshItems();
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