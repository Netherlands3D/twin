using System;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.UI
{
    public enum CanvasType { World, UI, Overlay }
    public class CanvasID : MonoBehaviour
    {        
        public CanvasType type;
        private GameObject originalParent;
        private bool wasActiveInOriginalHierarchy;

        private void Start()
        {
            if (GetComponent<Canvas>() != null) return;

            CanvasID[] canvasses = FindObjectsByType<CanvasID>(FindObjectsSortMode.None);
            foreach (CanvasID canvasID in canvasses)
            {
                if (canvasID.type == type)
                {
                    originalParent = transform.parent.gameObject;
                    wasActiveInOriginalHierarchy = gameObject.activeInHierarchy;
                    ActivationTracker tracker = originalParent.GetComponent<ActivationTracker>();
                    if (!tracker)
                        tracker = originalParent.AddComponent<ActivationTracker>();

                    tracker.OnActivated += OnOriginalParentBecameActive;
                    tracker.OnDeactivated += OnOriginalParentBecameInactive;
                  
                    transform.SetParent(canvasID.transform, true);
                    gameObject.SetActive(wasActiveInOriginalHierarchy);
                    break;
                }
            }
        }

        private void OnOriginalParentBecameActive()
        {
            gameObject.SetActive(true);
        }

        private void OnOriginalParentBecameInactive()
        {
            gameObject.SetActive(false);
        }

        private class ActivationTracker : MonoBehaviour
        {
            public event Action OnActivated;
            public event Action OnDeactivated;

            private void OnEnable()
            {
                OnActivated?.Invoke();
            }

            private void OnDisable()
            {
                OnDeactivated?.Invoke();
            }

        }
    }
}
