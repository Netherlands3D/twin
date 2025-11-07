using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Projects;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
{
    public class LayerToolBarButtonAddFolder : LayerToolBarButtonBase
    {
        public override void ButtonAction()
        {
            CreateFolderLayer();
        }

        public override void OnDrop(PointerEventData eventData)
        {
            GroupSelectedLayers();
        }
        
        private async Task<LayerData> CreateFolderLayer()
        {
            var builder = new LayerBuilder().OfType("folder").NamedAs("Folder");
            var folder = await App.Layers.Add(builder);
            return folder.LayerData;
        }
        
        private async void GroupSelectedLayers()
        {
            if (ProjectData.Current.RootLayer.SelectedLayers.Count == 0) 
                return;
            
            var newGroup = await CreateFolderLayer();
            var referenceLayer = ProjectData.Current.RootLayer.SelectedLayers.Last();
            newGroup.SetParent(referenceLayer.ParentLayer, referenceLayer.SiblingIndex);
            var layersToGroup = layerUIManager.GetLayersSortedByUI();
            foreach (var selectedLayer in layersToGroup)
            {
                selectedLayer.SetParent(newGroup);
            }
        }
    }
}