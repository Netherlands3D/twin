using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Legend;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;
using ScrollView = UnityEngine.UIElements.ScrollView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class TimelineLegendPanel : VisualElement
    {
        private Label emptyTextLabel;
        private ScrollView imageContainer;
        private ListView listView;
        
        public LegendUrlContainer activeLegendUrlContainer;
        public bool Visible => !ClassListContains(UtilityClassConstants.HIDDEN);

        private ColorPropertyData colorPropertyData;
        private TimelineStatusStylingLayerPropertyData timelineStatusStylingPropertyData;

        public TimelineLegendPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            // emptyTextLabel = this.Q<Label>("EmptyText");
            imageContainer = this.Q<ScrollView>("ImageListView");
            
            this.Q<ContentContainer>().CloseButtonClicked.AddListener(Close);
            
            listView = contentContainer.Q<ListView>("StatusList");
            
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.selectionType = SelectionType.None;
            
            listView.makeItem = MakeListViewItem;
            listView.bindItem = BindListViewItem;

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            colorPropertyData?.OnStylingChanged.RemoveListener(UpdateSwatches);
            timelineStatusStylingPropertyData?.OnStylingChanged.RemoveListener(UpdateSwatches);
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
            
            string status = listView.itemsSource[index] as string;
            Color? color;
            bool? visibility;
            if (index > 0)
            {
                color = timelineStatusStylingPropertyData.GetColorForStatus(status);
                visibility = timelineStatusStylingPropertyData.GetVisibilityForStatus(status);
            }
            else
            {
                color = colorPropertyData.GetDefaultSymbolizerColor();
                visibility = colorPropertyData.GetDefaultSymbolizerVisibility();
            }
            tile.Tile.ColorHex = color.HasValue ? ColorUtility.ToHtmlStringRGB(color.Value) : ColorUtility.ToHtmlStringRGB(Color.white);
            tile.Tile.LabelText = status;
            tile.VisibilityState = visibility.HasValue ? visibility.Value : true;
        }
        
        private void UpdateSwatches()
        {
            var list = new List<string>() { "Geen status" };
            list.AddRange(timelineStatusStylingPropertyData.GetStylingRuleNames());
            listView.itemsSource = list;
            listView.RefreshItems();
        }
        
        public void SetVisible(bool show)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }

        private void ShowEmptyText(bool show)
        {
            emptyTextLabel.EnableInClassList(UtilityClassConstants.HIDDEN, !show);
        }

        private void Close()
        {
            SetVisible(false);
        }

        public void SetLegend(LayerData layerData)
        {
            colorPropertyData = layerData.GetProperty<ColorPropertyData>();
            colorPropertyData.OnStylingChanged.AddListener(UpdateSwatches);
            timelineStatusStylingPropertyData = layerData.GetProperty<TimelineStatusStylingLayerPropertyData>();
            timelineStatusStylingPropertyData.OnStylingChanged.AddListener(UpdateSwatches); //todo: do this once per frame
            UpdateSwatches();
        }
    }
}