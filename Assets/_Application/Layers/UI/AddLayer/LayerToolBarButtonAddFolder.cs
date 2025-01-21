using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
{
    public class LayerToolBarButtonAddFolder : LayerToolBarButtonBase
    {
        public override void ButtonAction()
        {
            layerUIManager.CreateFolderLayer();
        }

        public override void OnDrop(PointerEventData eventData)
        {
            layerUIManager.GroupSelectedLayers();
        }
    }
}