using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class LayerToolBarButtonAddFolder : LayerToolBarButtonBase
    {
        public override void ButtonAction()
        {
            layerUIManager.CreateFolderLayer();
        }

        public override void OnDrop(PointerEventData eventData)
        {
            if (layerUIManager.ProjectData.RootLayer.SelectedLayers.Count > 0) //todo: replace layerManager reference with ProjectData reference
                layerUIManager.GroupSelectedLayers();
        }
    }
}