using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class HideObjectPanel : VisualElement
    {
        public UnityEvent OnClose = new();
        
        private ListView listView;
        private ListView ListView => listView ??= this.Q<ListView>();
        
        private Button button;
        public Button Button => button ??= this.Q<Button>("HideButton");

        public HideObjectPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
        }
        
        public HideObjectPanel(Dictionary<string, object> data) :  this()
        {
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;
            
            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;

            //todo leave this temp keys test code until meerdere gebouwen is merged
            // List<string> keys = new List<string>()
            // {
            //     "test", "test", "test", "test", "test", "test", "test", "test", "test", "test", "test", "test", "test",
            //     "test", "test", "test", "test", "test", "test", "test", "test", "test", "test", "test", "test"
            // };
            // PopulateBagIds(keys);
            PopulateBagIds(data.Keys.ToList());

            Button.clicked += OnClose.Invoke;
        }
        
        ~HideObjectPanel()
        {
            Button.clicked -= OnClose.Invoke;
        }

        public void PopulateBagIds(List<string> mappings)
        {
            ListView.itemsSource = mappings;
            ListView.RefreshItems();
        }
        
        private VisualElement MakeListViewItem()
        {
            return new HideObjectListViewItem();
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not HideObjectListViewItem listViewItem) return;
            
            string mapping = ListView.itemsSource[index] as string;
            listViewItem.ID = mapping;
        }
    }
}