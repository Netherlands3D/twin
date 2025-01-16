using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
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