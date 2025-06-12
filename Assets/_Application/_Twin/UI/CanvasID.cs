using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.UI
{
    public enum CanvasType { World, UI, Overlay }
    public class CanvasID : MonoBehaviour
    {        
        public CanvasType type;
        public int renderOrder = 0;
        private GameObject originalParent;
        private bool wasActiveInOriginalHierarchy;


        private void Start()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null) 
            {
                canvas.sortingOrder = (int)type;
                return;
            }
            //if this object is not a canvas it means it this object needs to be moved to its corresponding canvas 
            canvas = GetCanvasByType(type);
            if(canvas == null)
            {
                Debug.LogError($"expected a canvas with type {type} but none was found!");
                return;
            }

            originalParent = transform.parent.gameObject;
            wasActiveInOriginalHierarchy = gameObject.activeInHierarchy;
            ActivationTracker tracker = originalParent.GetComponent<ActivationTracker>();
            if (!tracker)
                tracker = originalParent.AddComponent<ActivationTracker>();

            tracker.OnActivated += OnOriginalParentBecameActive;
            tracker.OnDeactivated += OnOriginalParentBecameInactive;

            transform.SetParent(canvas.transform, true);
            gameObject.SetActive(wasActiveInOriginalHierarchy);
            SortSiblingsByOrder();
        }

        private void OnOriginalParentBecameActive()
        {
            gameObject.SetActive(true);
        }

        private void OnOriginalParentBecameInactive()
        {
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            SortSiblingsByOrder();
        }

        private void SortSiblingsByOrder()
        {
            var siblings = transform.parent
                .GetComponentsInChildren<CanvasID>(includeInactive: true)
                .OrderBy(c => c.renderOrder)
                .ToArray();

            for (int i = 0; i < siblings.Length; i++)
            {
                siblings[i].transform.SetSiblingIndex(i);
            }
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

        public static Canvas GetCanvasByType(CanvasType type)
        {
            CanvasID[] canvasses = FindObjectsByType<CanvasID>(FindObjectsSortMode.None);
            foreach (CanvasID canvasID in canvasses)
            {
                if (canvasID.type == type)
                {
                    Canvas canvas = canvasID.GetComponent<Canvas>();
                    if (canvas != null) //this check is required because it should check all iterations until the canvas is found
                        return canvas;
                }
            }
            return null;
        }
    }
}
