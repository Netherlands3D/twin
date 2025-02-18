using Netherlands3D.Twin.Layers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Tools.UI
{
    public class OverlayInstantiator : MonoBehaviour
    {
        [SerializeField] private List<OverlayInspector> overlayPrefabs;
        
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
            if(clearExistingContent)
                ContentOverlayContainer.Instance.ClearAllOverlays();

            foreach (OverlayInspector overlay in overlayPrefabs)
            {
                var spawnedOverlay = ContentOverlayContainer.Instance.AddOverlay(overlay);
                if (layerGameObject != null)
                    spawnedOverlay.SetReferencedLayer(layerGameObject);
            }
        }
    }
}
