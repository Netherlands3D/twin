using UnityEngine;

namespace Netherlands3D.Twin
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
            ContentOverlay.Instance.CloseOverlay();
        }
    }
}
