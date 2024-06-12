using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class OverlayInstantiator : MonoBehaviour
    {
        [SerializeField] private OverlayInspector overlayPrefab;

        public void InstantiateOverlay(bool clearExistingContent)
        {
            ContentOverlay.Instance.ShowOverlay(overlayPrefab, clearExistingContent);
        }
    }
}
