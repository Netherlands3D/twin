using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin;
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
            listView = this.Q<ListView>();

            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.selectionType = SelectionType.None;

            listView.makeItem = MakeListViewItem;
            listView.bindItem = BindListViewItem;
            
            mappings = data;
           
            UpdateContent();

            Button.clicked += OnClose.Invoke;
            
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Button.clicked -= OnClose.Invoke;
        }

        public void UpdateContent()
        {
            listView.itemsSource = mappings.Keys.ToList();
            listView.RefreshItems();
        }
        
        private VisualElement MakeListViewItem()
        {
            HideObjectListViewItem item = new HideObjectListViewItem();
            item.ShowToggle(false);
            item.RegisterCallback<PointerDownEvent>(_ =>
            {
                //move to coord
                string id = item.ID;
                if (mappings[id] is not MeshMapping map) return;
              
                Coordinate coord = map.GetCoordinateForObjectMappingItem(map.ObjectMapping, map.ObjectMapping.items[id]);
                App.Cameras.ActiveCamera.GetComponent<MoveCameraToCoordinate>().LookAtTarget(coord, cameraDistance);
            });
            var listViewItem = new ListViewItem(item);
            return listViewItem;
        }
        
        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<HideObjectListViewItem>() is not HideObjectListViewItem element) return;

            string mapping = listView.itemsSource[index] as string;
            element.ID = mapping;
        }
    }
}