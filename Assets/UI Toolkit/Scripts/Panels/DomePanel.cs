using System.Collections.Generic;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using ListView = Netherlands3D.UI.Components.ListView;


namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DomePanel : FloatingPanel
    {
        private ListView listView;
        private ListView ListView => listView ??= this.Q<ListView>();
        
        private Button button;
        private Button Button => button ??= this.Q<Button>("HideButton");

        public override void Initialize(Vector2 screenPosition, Dictionary<string, object> data)
        {
            base.Initialize(screenPosition, data);

            // Virtualization and selection
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;

            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;

            Button.clicked += () =>
            {
                OnClose.Invoke();
            };
        }

        private VisualElement MakeListViewItem()
        {
            var button = new Button { name = "ToggleHidden" };
            var listViewItem = new ListViewItem(button);
            
            return listViewItem;
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<Button>() is not Button button) return;
            
            IMapping mapping = ListView.itemsSource[index] as IMapping;
            button.LabelText = mapping.Id;
            var icon = IconImage.Map;
            button.Image = icon;
            button.userData = mapping;
        }
    }
}