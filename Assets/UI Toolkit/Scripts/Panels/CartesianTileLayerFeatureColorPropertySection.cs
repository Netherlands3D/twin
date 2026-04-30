using System.Collections.Generic;
using System.Linq;
using Netherlands3D.LayerStyles;
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
    [PropertySection(typeof(CartesianTileLayerFeatureColorPropertyData))]
    public partial class CartesianTileLayerFeatureColorPropertySection : VisualElement, IVisualizationWithPropertyData, IPropertyPanelWithColorPicker
    {
        public ColorPicker ColorPicker { get; set; }
        
        private CartesianTileLayerFeatureColorPropertyData stylingPropertyData;
        private ListView swatchesListView;
        private ListView SwatchesListView => swatchesListView ??= this.Q<ListView>();
        private List<string> colors = new();
        
        public CartesianTileLayerFeatureColorPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            SwatchesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            SwatchesListView.selectionType = SelectionType.Multiple;
            
            SwatchesListView.makeItem = MakeListViewItem;
            SwatchesListView.bindItem = BindListViewItem;
            
            
            SwatchesListView.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                //when clicked outside the listview, deselect the current selection
                SwatchesListView.RegisterCallback<BlurEvent>(evt =>
                {
                    var pos = Pointer.current.position.ReadValue();
                    var panelPos = RuntimePanelUtils.ScreenToPanel(
                        SwatchesListView.panel,
                        new Vector2(pos.x, Screen.height - pos.y)
                    );
                    if (!SwatchesListView.worldBound.Contains(panelPos) && !ColorPicker.worldBound.Contains(panelPos))
                    {
                        SwatchesListView.ClearSelection();
                    }
                });
            });
            
            SwatchesListView.selectedIndicesChanged += indices =>
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
            return item;
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ColorTileListViewItem listViewItem) return;
           
            string color = SwatchesListView.itemsSource[index] as string;
            listViewItem.Tile.ColorHex = color;
            
            string layerName = stylingPropertyData.GetStylingRuleNameByMaterialIndex(index);
            listViewItem.Tile.LabelText = layerName;
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.GetDefaultStylingPropertyData<CartesianTileLayerFeatureColorPropertyData>();
            if (stylingPropertyData == null) return;
            
            UpdateSwatches();
            
            stylingPropertyData.OnStylingChanged.AddListener(UpdateSwatches);
            ColorPicker.ColorSelected.AddListener(OnPickColor);

            ColorPicker.SetVisible(false);
        }

        private void OnDestroy()
        {
            stylingPropertyData.OnStylingChanged.RemoveListener(UpdateSwatches);
            ColorPicker.ColorSelected.RemoveListener(OnPickColor);
        }


        private void UpdateSwatches()
        {
            colors.Clear();
            foreach(KeyValuePair<string, StylingRule> kv in stylingPropertyData.StylingRules)
            {
                if(kv.Key.Contains(CartesianTileLayerFeatureColorPropertyData.ColoringIdentifier))
                {
                    int index = stylingPropertyData.GetMaterialIndexFromStyleRuleKey(kv.Key);                    
                    Color? color = stylingPropertyData.GetColorByMaterialIndex(index);
                    //we need to expect a value here or else the stylingrule is not properly initialized
                    if (color.HasValue)
                    {
                        colors.Add(ColorUtility.ToHtmlStringRGB(color.Value));
                    }
                    else
                        Debug.LogError("stylingrule not initialized because the colorvalue is missing");
                }
            }
            SwatchesListView.itemsSource = colors;
            SwatchesListView.RefreshItems();
        }

        private void OnPickColor(Color color)
        {
            //since we apply styling to multiple stylingrules we have to use the notify == false and invoke styling changed afterwards
            foreach (int i in SwatchesListView.selectedIndices)
            {
                string layerName = stylingPropertyData.GetStylingRuleNameByMaterialIndex(i);
                stylingPropertyData.SetColorByMaterialIndex(i, layerName, color, false);
            }
            stylingPropertyData.OnStylingChanged.Invoke();
        }
    }
}