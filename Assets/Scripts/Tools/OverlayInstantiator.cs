using Netherlands3D.Twin.Layers;
using UnityEngine;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class OverlayInstantiator : MonoBehaviour
    {
        [SerializeField] private OverlayInspector overlayPrefab;
        
        [FormerlySerializedAs("referencedLayer")]
        [Header("(Optional)")]
        [SerializeField] private LayerGameObject layerGameObject;
        [SerializeField] private bool instantiateOnStart = false;
        
        private void Start() 
        {   
            if(instantiateOnStart)
                InstantiateOverlay(true);
        }

        public void InstantiateOverlay(bool clearExistingContent = true)
        {
            var spawnedOverlay = ContentOverlayContainer.Instance.ShowOverlay(overlayPrefab, clearExistingContent);

            if(layerGameObject != null)
                spawnedOverlay.SetReferencedLayer(layerGameObject);
        }
    }
}
