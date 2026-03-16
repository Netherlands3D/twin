using System.Linq;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;
using ListView = Netherlands3D.UI.Components.ListView;


namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DomePanel : FloatingPanel
    {
        private ListView listView;
        private ListView ListView => listView ??= this.Q<ListView>();

        public override void Initialize(Vector2 screenPosition, object context = null)
        {
            base.Initialize(screenPosition, context);
            
            ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ListView.selectionType = SelectionType.None;

            ListView.makeItem = MakeListViewItem;
            ListView.bindItem = BindListViewItem;
            
            PopulateMaskLayerPanel();
        }
        
        private VisualElement MakeListViewItem()
        {
            var rowElement = new MaskLayerRowElement();
            var listViewItem = new ListViewItem(rowElement);
            rowElement.MaskActiveToggle.RegisterCallback<ClickEvent>(_ => Debug.Log("toggle mask active: " +  rowElement.name));
            
            return listViewItem;
        }

        private void BindListViewItem(VisualElement item, int index)
        {
            if (item is not ListViewItem listViewItem) return;
            if (listViewItem.Q<MaskLayerRowElement>() is not MaskLayerRowElement maskLayerRowElement) return;
            
            var layerData = ListView.itemsSource[index] as LayerData;
            var layerPropertyData = layerData.GetProperty<MaskingLayerPropertyData>();
            if (layerPropertyData == null)
                return; //unmaskable layer
            
            maskLayerRowElement.LayerName = layerData.Name;
            maskLayerRowElement.ToggleIsOn = GetIsDomeMaskingBitSet(layerPropertyData);

            maskLayerRowElement.userData = layerData;
        }

        private bool GetIsDomeMaskingBitSet(MaskingLayerPropertyData layerPropertyData)
        {
            var currentLayerMask = layerPropertyData.GetMaskLayerMask();
            int maskBitToCheck = 1 << MaskingLayerPropertyData.MASKING_DOME_BIT_INDEX;
            bool isBitSet = (currentLayerMask & maskBitToCheck) != 0;
            return isBitSet;
        }

        private void PopulateMaskLayerPanel()
        {
            var layers = ProjectData.Current.RootLayer.GetFlatHierarchy(); //todo: should this be the context passed by the Initialize function?
            ListView.itemsSource = layers.ToList();
            ListView.RefreshItems();
        }
    }
}