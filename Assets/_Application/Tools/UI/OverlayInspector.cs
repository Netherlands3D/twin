using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin.Tools.UI
{
    public class OverlayInspector : MonoBehaviour
    {
        private LayerGameObject layerGameObject;
        public LayerGameObject LayerGameObject { get => layerGameObject; }

        public virtual void SetReferencedLayer(LayerGameObject layerGameObject)
        {
            this.layerGameObject = layerGameObject;
        }

        public virtual void CloseOverlay()
        {
            ContentOverlayContainer.Instance.CloseOverlay();
        }
    }
}
