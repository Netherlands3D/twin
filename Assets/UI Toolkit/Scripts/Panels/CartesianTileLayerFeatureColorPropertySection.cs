using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(CartesianTileLayerFeatureColorPropertyData), PropertySectionCategory.Styling)]
    public partial class CartesianTileLayerFeatureColorPropertySection : VisualElement, IVisualizationWithPropertyData, IPropertyPanelWithColorPicker
    {
        public ColorPicker ColorPicker { get; set; }
        
        private CartesianTileLayerFeatureColorPropertyData stylingPropertyData;
        private ListView swatchesListView;
        private List<CartesianTileLayerFeatureColorPropertyData.ColorData> colorData = new();
        
        public CartesianTileLayerFeatureColorPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            swatchesListView = this.Q<ListView>();
            
            swatchesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            swatchesListView.selectionType = SelectionType.Multiple;
            
            swatchesListView.makeItem = MakeListViewItem;
            swatchesListView.bindItem = BindListViewItem;
            
            
            //when clicked outside the listview, deselect the current selection
            swatchesListView.RegisterCallback<BlurEvent>(evt =>
            {
                var pos = Pointer.current.position.ReadValue();
                var panelPos = RuntimePanelUtils.ScreenToPanel(
                    swatchesListView.panel,
                    new Vector2(pos.x, Screen.height - pos.y)
                );
                if (!swatchesListView.worldBound.Contains(panelPos) && !ColorPicker.worldBound.Contains(panelPos))
                {
                    swatchesListView.ClearSelection();
                }
            });
            
            swatchesListView.selectedIndicesChanged += indices =>
            {
                //show selection in world when items in panel are selected
                ColorPicker.SetVisible(indices.Any());
            };
            
            RegisterCallback<DetachFromPanelEvent>(_ => 
            {
                OnDestroy();  
            });
        }
        
        private VisualElement MakeListViewItem()
        {
            ColorTileListViewItem item = new();
            item.Tile.ShowLabel = true;
            item.RegisterCallback<ClickEvent>(evt =>
            {
                ColorPicker.SetColorInputComponentsWithoutNotify(item.Tile.Color);
            });
            var listViewItem = new ListViewItem(item);
            return listViewItem;
            //return item;
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<ColorTileListViewItem>() is not ColorTileListViewItem tile) return;
            //if (item.Q<ColorTileListViewItem>() is not ColorTileListViewItem tile) return;
           
            string color = swatchesListView.itemsSource[index] as string;
            tile.Tile.ColorHex = color;
            
            string layerName = stylingPropertyData.GetStylingRuleNameByMaterialIndex(index);
            //layer names usually will look like Twin_Something, lets use only the second part of the split on _
            string name = layerName.Contains('_') ? layerName.Split('_')[1] : layerName;
            tile.Tile.LabelText = name;
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.GetDefaultStylingPropertyData<CartesianTileLayerFeatureColorPropertyData>();
           
            UpdateSwatches();
            
            stylingPropertyData.OnStylingChanged.AddListener(UpdateSwatches);
            ColorPicker.ColorChanged.AddListener(OnPickColor);

            ColorPicker.SetVisible(false);
        }

        private void OnDestroy()
        {
            stylingPropertyData.OnStylingChanged.RemoveListener(UpdateSwatches);
            ColorPicker.ColorChanged.RemoveListener(OnPickColor);
        }


        private void UpdateSwatches()
        {
            swatchesListView.itemsSource = stylingPropertyData.GetUsedColorTypes();
            swatchesListView.RefreshItems();
        }
        
        private void OnPickColor(Color color)
        {
            colorData.Clear();
            //since we apply styling to multiple stylingrules we have to use the notify == false and invoke styling changed afterwards
            foreach (int i in swatchesListView.selectedIndices)
            {
                string layerName = stylingPropertyData.GetStylingRuleNameByMaterialIndex(i);
                CartesianTileLayerFeatureColorPropertyData.ColorData data = new();
                data.index = i;
                data.name = layerName;
                data.color = color;
                colorData.Add(data);
            }
            stylingPropertyData.SetColorsByMaterialIndices(colorData);
        }
    }
}