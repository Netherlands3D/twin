using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.Functionalities.LASImporter
{
    [UxmlElement]
    [PropertySection(typeof(LASClassificationColorPropertyData), PropertySectionCategory.Styling)]
    public partial class LASClassificationPropertySection : VisualElement, IVisualizationWithPropertyData, IPropertyPanelWithColorPicker
    {
        private LASClassificationColorPropertyData propertyData;
        private ListView classificationListView;

        public ColorPicker ColorPicker { get; set; }

        public LASClassificationPropertySection()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            classificationListView = this.Q<ListView>("ClassificationList");
            classificationListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            classificationListView.selectionType = SelectionType.Multiple;
            classificationListView.makeItem = MakeListViewItem;
            classificationListView.bindItem = BindListViewItem;

            classificationListView.RegisterCallback<BlurEvent>(OnListViewBlurred);
            classificationListView.selectedIndicesChanged += OnSelectedIndicesChanged;

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private VisualElement MakeListViewItem()
        {
            var item = new ColorTileListViewItem();
            item.Tile.ShowLabel = true;
            item.RegisterCallback<ClickEvent>(_ =>
            {
                if (item.userData is byte classification)
                    ColorPicker.SetColorInputComponentsWithoutNotify(GetClassificationColor(classification));
            });

            return new ListViewItem(item);
        }

        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<ColorTileListViewItem>() is not ColorTileListViewItem tile) return;
            if (classificationListView.itemsSource[index] is not byte classification) return;

            var count = propertyData.GetCount(classification);
            tile.userData = classification;
            tile.Tile.LabelText = $"{propertyData.GetClassificationName(classification)} ({count})";
            tile.Tile.Color = GetClassificationColor(classification);
        }

        private void OnListViewBlurred(BlurEvent evt)
        {
            if (ColorPicker == null)
                return;

            var pos = Pointer.current.position.ReadValue();
            var panelPos = RuntimePanelUtils.ScreenToPanel(
                classificationListView.panel,
                new Vector2(pos.x, Screen.height - pos.y)
            );

            if (!classificationListView.worldBound.Contains(panelPos) && !ColorPicker.worldBound.Contains(panelPos))
                classificationListView.ClearSelection();
        }

        private void OnSelectedIndicesChanged(IEnumerable<int> indices)
        {
            var anySelected = indices.Any();
            ColorPicker.SetVisible(anySelected);

            if (!anySelected || classificationListView.selectedItem is not byte classification)
                return;

            ColorPicker.SetColorInputComponentsWithoutNotify(GetClassificationColor(classification));
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (propertyData != null)
                propertyData.OnStylingChanged.RemoveListener(UpdateSwatches);

            ColorPicker?.ColorChanged.RemoveListener(OnColorPicked);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            propertyData = properties.GetDefaultStylingPropertyData<LASClassificationColorPropertyData>();
            if (propertyData == null)
                return;

            propertyData.OnStylingChanged.AddListener(UpdateSwatches);
            ColorPicker.ColorChanged.AddListener(OnColorPicked);
            UpdateSwatches();
            ColorPicker.SetVisible(false);
        }

        private void UpdateSwatches()
        {
            classificationListView.itemsSource = propertyData.GetClassifications().ToList();
            classificationListView.RefreshItems();

            foreach (var index in classificationListView.selectedIndices)
            {
                var element = classificationListView.GetRootElementForIndex(index) as ListViewItem;
                var tile = element?.Q<ColorTileListViewItem>();
                if (tile?.userData is byte classification)
                    tile.Tile.Color = GetClassificationColor(classification);
            }
        }

        private Color GetClassificationColor(byte classification)
        {
            return propertyData.GetColorByClassification(classification).GetValueOrDefault(Color.white);
        }

        private void OnColorPicked(Color color)
        {
            foreach (var classification in classificationListView.selectedItems.OfType<byte>())
            {
                propertyData.SetColorByClassification(
                    classification,
                    propertyData.GetClassificationName(classification),
                    color
                );
            }
        }
    }
}
