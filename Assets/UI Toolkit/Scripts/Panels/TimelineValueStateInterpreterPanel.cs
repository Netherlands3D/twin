using System.Collections.Generic;
using System.Linq;
using Netherlands3D.LayerStyles;
using Netherlands3D.Timeline;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using ListView = UnityEngine.UIElements.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class TimelineValueStateInterpreterPanel : VisualElement
    {
        private TimelineStylingLayerPropertyData timelineStylingPropertyData;
        private TimestampValueStatusInterpreter Interpreter;

        private ListView listView;
        public ColorPicker ColorPicker { get; set; }
        
        public TimelineValueStateInterpreterPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
            
            listView = this.Q<ListView>("StatusList");
            
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.selectionType = SelectionType.Multiple;
            
            listView.makeItem = MakeListViewItem;
            listView.bindItem = BindListViewItem;
            
            //when clicked outside the listview, deselect the current selection
            listView.RegisterCallback<BlurEvent>(evt =>
            {
                var pos = Pointer.current.position.ReadValue();
                var panelPos = RuntimePanelUtils.ScreenToPanel(
                    listView.panel,
                    new Vector2(pos.x, Screen.height - pos.y)
                );
                if (!listView.worldBound.Contains(panelPos) && !ColorPicker.worldBound.Contains(panelPos))
                {
                    listView.ClearSelection();
                }
            });
            
            listView.selectedIndicesChanged += indices =>
            {
                //show selection in world when items in panel are selected
                ColorPicker.SetVisible(indices.Any());
            };
            
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            timelineStylingPropertyData.OnStylingChanged.RemoveListener(UpdateSwatches);
            ColorPicker.ColorChanged.RemoveListener(OnPickColor);
        }

        public TimelineValueStateInterpreterPanel(TimestampValueStatusInterpreter interpreter, ColorPicker colorPicker) : this()
        {
            Interpreter = interpreter;
            ColorPicker = colorPicker;
            ColorPicker.ColorChanged.AddListener(OnPickColor);
            listView.itemsSource = Interpreter.GetStates();
        }
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            timelineStylingPropertyData = properties.Get<TimelineStylingLayerPropertyData>();
            timelineStylingPropertyData.OnTimestampCollectionAdded.AddListener(OnTimestampCollectionAdded);
        }

        private void OnTimestampCollectionAdded(TimestampCollection newCollection)
        {
            Interpreter.ProcessNewCollection(newCollection);
            UpdateSwatches();
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
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<ColorTileListViewItem>() is not ColorTileListViewItem tile) return;
           
            string status = listView.itemsSource[index] as string;
            Color color = Interpreter.GetColorForStatus(status);
            tile.Tile.ColorHex = ColorUtility.ToHtmlStringRGB(color);
            tile.Tile.LabelText = status;
        }
        
        private void UpdateSwatches()
        {
            listView.itemsSource = Interpreter.GetStates();
            listView.RefreshItems();
        }
        
        private void OnPickColor(Color color)
        {
            foreach (int i in listView.selectedIndices)
            {
                string status = listView.itemsSource[i] as string;
                Interpreter.SetColorForStatus(status, color);
            }
        }
    }
}