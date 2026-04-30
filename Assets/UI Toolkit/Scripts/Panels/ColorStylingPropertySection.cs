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
    [PropertySection(typeof(ColorPropertyData))]
    public partial class ColorStylingPropertySection : VisualElement, IVisualizationWithPropertyData, IPropertyPanelWithColorPicker
    {
        private Color defaultColor = Color.white;
        private ColorPropertyData stylingPropertyData;
        private ListView swatchesListView;

        public ColorPicker ColorPicker { get; set; }

        public ColorStylingPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            swatchesListView = this.Q<ListView>();

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

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
        }

        private VisualElement MakeListViewItem()
        {
            ColorTileListViewItem item = new();
            item.Tile.ShowLabel = true;
            item.RegisterCallback<ClickEvent>(evt =>
            {
                stylingPropertyData.ColorType = item.Tile.LabelText;
                var defaultSymbolizerColor = stylingPropertyData.GetDefaultSymbolizerColor();
                var color = defaultSymbolizerColor.HasValue ? defaultSymbolizerColor.Value : defaultColor;
                ColorPicker.SetColorInputComponentsWithoutNotify(color);
            });
            return item;
        }

        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ColorTileListViewItem listViewItem) return;

            string propertyName = swatchesListView.itemsSource[index] as string;
            listViewItem.Tile.LabelText = propertyName;
            var defaultSymbolizerColor = stylingPropertyData.AnyFeature.Symbolizer.GetColor(propertyName);
            var color = defaultSymbolizerColor.HasValue ? defaultSymbolizerColor.Value : defaultColor;
            Debug.Log(propertyName + "\t" + color);
            listViewItem.Tile.Color = color;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            stylingPropertyData.OnStylingChanged.RemoveListener(UpdateColorFromProperty);
            ColorPicker.ColorChanged.RemoveListener(OnColorPicked);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            stylingPropertyData = properties.GetDefaultStylingPropertyData<ColorPropertyData>();

            if (stylingPropertyData == null) return;

            stylingPropertyData.OnStylingChanged.AddListener(UpdateColorFromProperty);
            ColorPicker.ColorChanged.AddListener(OnColorPicked);

            UpdateSwatches();
            UpdateColorFromProperty();
        }

        private void UpdateSwatches()
        {
            swatchesListView.itemsSource = new List<string>() { Symbolizer.FillColorProperty, Symbolizer.StrokeColorProperty };
            swatchesListView.RefreshItems();
        }


        private void UpdateColorFromProperty()
        {
            Color? colorValue = stylingPropertyData.GetDefaultSymbolizerColor();
            var color = colorValue.HasValue ? colorValue.Value : defaultColor;
            ColorPicker.SetColorInputComponentsWithoutNotify(color);
            foreach (var index in swatchesListView.selectedIndices)
            {
                var element = swatchesListView.GetRootElementForIndex(index) as ColorTileListViewItem;
                if (element != null)
                {
                    Debug.Log("updating tile color for: " + element.Tile.LabelText);
                    element.Tile.Color = color;
                }
            }
        }

        private void OnColorPicked(Color color)
        {
            foreach (var item in swatchesListView.selectedItems.OfType<string>())
            {
                Debug.Log("setting color for " + item);
                stylingPropertyData.ColorType = item;
                stylingPropertyData.SetDefaultSymbolizerColor(color);
            }
        }
    }
}