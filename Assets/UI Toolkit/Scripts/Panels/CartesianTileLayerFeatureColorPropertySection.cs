using System.Collections.Generic;
using System.Linq;
using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;
using Toggle = UnityEngine.UIElements.Toggle;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    [PropertySection(typeof(CartesianTileLayerFeatureColorPropertyData))]
    public partial class CartesianTileLayerFeatureColorPropertySection : VisualElement, IVisualizationWithPropertyData
    {
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
                RegisterOutsidePanelClick();
            });
            
            SwatchesListView.selectedIndicesChanged += indices =>
            {
                //show selection in world when items in panel are selected
                UpdateSelectionForIndices(indices);
            };
            
            RegisterCallback<DetachFromPanelEvent>(_ => 
            {
                OnDestroy();  
            });
        }
        
        private void RegisterOutsidePanelClick()
        {
            var pointerAction = new InputAction(binding: "<Pointer>/press");
            pointerAction.performed += _ =>
            {
                var pos = Pointer.current.position.ReadValue();
                var panelPos = RuntimePanelUtils.ScreenToPanel(
                    SwatchesListView.panel,
                    new Vector2(pos.x, Screen.height - pos.y)
                );
                    
                if (!SwatchesListView.worldBound.Contains(panelPos))
                {
                    SwatchesListView.ClearSelection();
                }
            };
            pointerAction.Enable();
    
            SwatchesListView.RegisterCallback<DetachFromPanelEvent>(_ => pointerAction.Dispose());
        }
        
        private VisualElement MakeListViewItem()
        {
            ColorTile item = new();
            item.RegisterCallback<PointerUpEvent>(evt =>
            {
                //open color wheel
            });
            return item;
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ColorTile listViewItem) return;
           
            string color = SwatchesListView.itemsSource[index] as string;
            listViewItem.Color = color;
        }
        
        private void UpdateSelectionForIndices(IEnumerable<int> indices)
        {
            // foreach (int i in indices)
            // {
            //     var id = ListView.itemsSource[i] as string;
            //     bool? visibility = stylingPropertyData.GetVisibilityForSubObjectById(id);
            //     if (visibility == true)
            //     {
            //         Coordinate coord = (Coordinate)stylingPropertyData.GetVisibilityCoordinateForSubObjectById(id);
            //         selector.SelectBagId(id, coord);
            //     }
            // }
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.GetDefaultStylingPropertyData<CartesianTileLayerFeatureColorPropertyData>();
            if (stylingPropertyData == null) return;
            
            
            UpdateSwatches();
            
            stylingPropertyData.OnStylingChanged.AddListener(UpdateSwatches);
            //colorPicker.ColorWheel.colorChanged.AddListener(OnPickColor);

            //HideColorPicker();
        }

        private void OnDestroy()
        {
            stylingPropertyData.OnStylingChanged.RemoveListener(UpdateSwatches);
            //colorPicker.ColorWheel.colorChanged.RemoveListener(OnPickColor);
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
                        //swatch.SetColor(color.GetValueOrDefault(Color.white));
                    }
                    else
                        Debug.LogError("stylingrule not initialized because the colorvalue is missing");
                }
            }
            SwatchesListView.itemsSource = colors;
            SwatchesListView.RefreshItems();
        }

        // private ColorSwatch CreateSwatch(int index)
        // {
        //     GameObject swatchObject = Instantiate(colorSwatchPrefab, layerContent);
        //     ColorSwatch swatch = swatchObject.GetComponent<ColorSwatch>();
        //
        //     string layerName = stylingPropertyData.GetStylingRuleNameByMaterialIndex(index);
        //         
        //     swatch.SetLayerName(layerName);
        //     swatch.SetInputText(layerName);
        //
        //     //because all ui elements will be destroyed on close an anonymous listener is fine here              
        //     swatch.onClickDown.AddListener(pointer => OnClickedOnSwatch(pointer, swatch));
        //
        //     return swatch;
        // }

        private void OnClickedOnSwatch(PointerEventData _, ColorSwatch swatch)
        {
            //select layer
            //SelectedButtonIndex = Items.IndexOf(swatch);
            // MultiSelectionUtility.ProcessLayerSelection(this, anySelected =>
            // {
            //     if(anySelected)
            //     {
            //         ShowColorPicker();
            //         colorPicker.PickColorWithoutNotify(((ColorSwatch)Items[SelectedButtonIndex]).Color);
            //     }
            //     else
            //     {
            //         HideColorPicker();
            //     }
            // });
        }

        // private void OnPickColor(Color color)
        // {
        //     foreach ((int index, ColorSwatch swatch) in swatches)
        //     {
        //         if (!swatch.IsSelected) continue;
        //         
        //         swatch.SetColor(color);
        //         stylingPropertyData.SetColorByMaterialIndex(index, swatch.LayerName, color);
        //     }
        // }
      

        // private void ShowColorPicker()
        // {
        //     colorPicker.gameObject.SetActive(true);
        //     colorPicker.LoadProperties(new List<LayerPropertyData>() { stylingPropertyData });
        // }
        //
        // private void HideColorPicker()
        // {
        //     colorPicker.gameObject.SetActive(false);
        // }       
    }
}