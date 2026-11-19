using System.Collections.Generic;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class TimelineLegendPanel : VisualElement
    {
        private LayerData activeLayer;
        private ListView listView;
        private TimelineStatusStylingLayerPropertyData timelineStatusStylingPropertyData;

        public TimelineLegendPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            this.Q<ContentContainer>().CloseButtonClicked.AddListener(Close);
            
            listView = contentContainer.Q<ListView>("StatusList");
            
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            
            listView.makeItem = MakeListViewItem;
            listView.bindItem = BindListViewItem;

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            foreach (var stylingProperty in activeLayer.GetProperties<StylingPropertyData>())
            {
                stylingProperty.OnStylingChanged.RemoveListener(UpdateSwatches);
            }
        }
        
        private VisualElement MakeListViewItem()
        {
            ColorTileListViewItem item = new();
            item.Tile.ShowLabel = true;
            var listViewItem = new ListViewItem(item);
            return listViewItem;
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<ColorTileListViewItem>() is not ColorTileListViewItem tile) return;
            
            var rule = listView.itemsSource[index] as StylingRule;
            
            string name =  rule.Name;
            
            if (name == StylingPropertyData.NameOfDefaultStyle)
            {
                name = "Geen status";
            }
            
            Color? color = rule.Symbolizer.GetColor(Symbolizer.FillColorProperty);//todo
            bool? visibility = rule.Symbolizer.GetVisibility();

            tile.Tile.ColorHex = color.HasValue ? ColorUtility.ToHtmlStringRGB(color.Value) : ColorUtility.ToHtmlStringRGB(Color.white);
            tile.Tile.LabelText = name;
            tile.Tile.VisibilityState = visibility.HasValue ? visibility.Value : true;
        }
        
        
        
        private void UpdateSwatches()
        {
            var list = new List<StylingRule>();
            foreach (var stylingPropertyData in activeLayer.GetProperties<StylingPropertyData>())
            {
                if(stylingPropertyData is MaskingLayerPropertyData) // Masking data should not be displayed in the legend
                    continue;

                    foreach (var rule in stylingPropertyData.StylingRules.Values)
                        list.Add(rule);
            }
            
            listView.itemsSource = list;
            listView.RefreshItems();
        }
        
        public void SetVisible(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }

        private void Close()
        {
            SetVisible(false);
        }
        
        public void SetLegend(LayerData layerData)
        {
            activeLayer = layerData;
            foreach (var stylingProperty in layerData.GetProperties<StylingPropertyData>())
            {
                stylingProperty.OnStylingChanged.AddListener(UpdateSwatches);
            }
            
            UpdateSwatches();
        }
    }
}