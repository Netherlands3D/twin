using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Tools.UI
{
    public class ContentOverlayContainer : MonoBehaviour
    {
        public static ContentOverlayContainer Instance { get; private set; }
        [SerializeField] private Image background;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Multiple ContentOverlay instances found, destroying this one", this);
                Destroy(gameObject);
                return;
            }

            ClearAllOverlays();
            Instance = this;
        }

        public OverlayInspector AddOverlay(OverlayInspector overlay)
        {
            var newOverlay = Instantiate(overlay, transform);
            background.enabled = true;            
            return newOverlay;
        }

        public void ClearAllOverlays()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            background.enabled = false;
        }

        public void CloseOverlay()
        {
            ClearAllOverlays(); //todo: allow stacked overlays?
        }
    }
}
