using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ContentOverlay : MonoBehaviour
    {
        public static ContentOverlay Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Multiple ContentOverlay instances found, destroying this one", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ShowOverlay(GameObject overlay, bool clear)
        {
            if(clear)
                ClearAllOverlays();

            Instantiate(overlay, transform);
        }

        private void ClearAllOverlays()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
