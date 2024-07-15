using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class LayerToolBarButtonDeleteLayer : LayerToolBarButtonBase
    {
        public override void ButtonAction()
        {
            layerUIManager.DeleteSelectedLayers();
        }

        public override void OnDrop(PointerEventData eventData)
        {
            layerUIManager.DeleteSelectedLayers();
        }
    }
}