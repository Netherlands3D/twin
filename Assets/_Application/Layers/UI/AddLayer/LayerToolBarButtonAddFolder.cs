using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Netherlands3D.Twin.Layers.LayerPresets;
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
        
        private LayerData CreateFolderLayer()
        {
            var folder = App.Layers.Add(new FolderPreset.Args("Folder"));
            return folder.LayerData;
        }
        
        //todo move this non ui logic into a service
        private void GroupSelectedLayers()
        {
            if (ProjectData.Current.RootLayer.SelectedLayers.Count == 0) 
                return;
            
            var newGroup = CreateFolderLayer();
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