using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class OverlayInspector : MonoBehaviour
    {
        private ReferencedLayer referencedLayer;
        public ReferencedLayer ReferencedLayer { get => referencedLayer; }

        public virtual void SetReferencedLayer(ReferencedLayer layer)
        {
            referencedLayer = layer;
        }

        public virtual void CloseOverlay()
        {
            ContentOverlayContainer.Instance.CloseOverlay();
        }
    }
}
