using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
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
        private Button Button => button ??= this.Q<Button>("HideButton");

        private float cameraDistance = 150f;

        private Dictionary<string, IMapping> mappings;

        public HideObjectPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");
        }
        
        public HideObjectPanel(Dictionary<string, IMapping> data) :  this()
        {
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;
            
            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;
           
            PopulateBagIds(data.Keys.ToList());

            mappings = data;

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
            HideObjectListViewItem listViewItem = new HideObjectListViewItem();
            listViewItem.ShowToggle(false);
            listViewItem.RegisterCallback<PointerDownEvent>(_ =>
            {
                //move to coord
                string id = listViewItem.ID;
                if (mappings[id] is not MeshMapping map) return;
              
                Coordinate coord = map.GetCoordinateForObjectMappingItem(map.ObjectMapping, map.ObjectMapping.items[id]);
                Camera.main.GetComponent<MoveCameraToCoordinate>().LookAtTarget(coord, cameraDistance);
            });
            return listViewItem;
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not HideObjectListViewItem listViewItem) return;
            
            string mapping = ListView.itemsSource[index] as string;
            listViewItem.ID = mapping;
        }
    }
}