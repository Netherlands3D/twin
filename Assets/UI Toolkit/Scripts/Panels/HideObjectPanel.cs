using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using ListView = Netherlands3D.UI.Components.ListView;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class HideObjectPanel : FloatingPanel
    {
        private ListView listView;
        private ListView ListView => listView ??= this.Q<ListView>();
        
        private Button button;
        private Button Button => button ??= this.Q<Button>("HideButton");

        public override void Initialize(Vector2 screenPosition, Dictionary<string, object> data)
        {
            base.Initialize(screenPosition, data);
            
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;
            
            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;

            //TODO should this be in the base for any floating panel?
            Button.clicked += () =>
            {
                OnClose.Invoke();
            };
            
            PopulateBagIds(data.Keys.ToList());
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