
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Catalogs;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Netherlands3D.UI.Components.Button;
using ListView = Netherlands3D.UI.Components.ListView;


namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class HideObjectPanel : FloatingPanel
    {
        private List<IMapping> mappings;
        private ListView listView;
        private ListView ListView => listView ??= this.Q<ListView>();
        private Button hideButton;

        public override void Initialize(Vector2 screenPosition, object context = null)
        {
            base.Initialize(screenPosition, context);

            mappings = context as List<IMapping>;
            if (mappings == null) return;

            // Virtualization and selection
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;

            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;

            // Hide button
            hideButton = this.Q<Button>("HideButton");
            if (hideButton != null)
            {
                hideButton.clicked += () =>
                {
                    HideMappings(mappings);
                    parent.Remove(this); // close panel after hiding
                };
            }
        }

        private VisualElement MakeListViewItem()
        {
            var button = new Button { name = "ToggleHidden" };
            var listViewItem = new ListViewItem(button);
            //button.RegisterCallback<ClickEvent>();
            
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
        
        private void HideMappings(List<IMapping> mappings)
        {
            
        }
    }
}