using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class LayerToolBarButtonAddLayer : LayerToolBarButtonBase
    {
        public override void ButtonAction()
        {
            layerManager.EnableContextMenu(true, transform.position);
        }

        public override void OnDrop(PointerEventData eventData)
        {
            Debug.LogError("duplicate layer is currently not implemented");
            throw new System.NotImplementedException();
        }
    }
}
