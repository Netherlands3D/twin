using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Netherlands3D.Twin
{
    public class TextTicker : MonoBehaviour
    {
        public RectTransform childRect; // Assign the child RectTransform in the inspector  
        public float scrollSpeed = 200f; // Speed of scrolling

        private RectTransform parentRect;
        private float childWidth;
        private float parentWidth;
        private Coroutine scrollingCoroutine;

        void Awake()
        {
            parentRect = GetComponent<RectTransform>();
        }

        void Start()
        {
            // Get initial widths and set initial position  
            UpdateWidths();
            SetInitialPosition();

            // You can call this method to start monitoring for width changes  
            StartMonitoringWidth();
        }

        private void SetInitialPosition()
        {
            if (childWidth > parentWidth)
            {
                childRect.anchoredPosition = new Vector2(0, childRect.anchoredPosition.y);
            }
        }

        private void UpdateWidths()
        {
            // Get widths of parent and child RectTransforms  
            childWidth = childRect.rect.width;
            parentWidth = parentRect.rect.width;
        }

        // Start monitoring for width changes  
        public void StartMonitoringWidth()
        {
            UpdateWidths(); // Initial check 
            CheckAndStartScrolling(); // Check and start scrolling if necessary  
        }

        // Example event trigger method to be called when needed  
        public void OnWidthChange() // Call this method when a width change occurs  
        {
            Invoke("UpdateWidths",0.1f);
            Invoke("CheckAndStartScrolling",0.1f);
        }

        private void CheckAndStartScrolling()
        {
            if (childWidth > parentWidth)
            {
                if (scrollingCoroutine == null)
                {
                    scrollingCoroutine = StartCoroutine(ScrollChild());
                }
            }
            else
            {
                StopScrolling();
            }
        }

        private IEnumerator ScrollChild()
        {
            while (true)
            {
                Vector2 newPosition = childRect.anchoredPosition;
                newPosition.x -= scrollSpeed * Time.deltaTime;

                // If the child has scrolled off the left side, reset its position  
                if (newPosition.x < -childWidth)
                {
                    newPosition.x = parentWidth; // Reset to the right edge of the parent  
                }

                childRect.anchoredPosition = newPosition;

                yield return null; // Wait for the next frame  
            }
        }

        private void StopScrolling()
        {
            if (scrollingCoroutine != null)
            {
                StopCoroutine(scrollingCoroutine);
                scrollingCoroutine = null;
                // Reset position when stopping  
                childRect.anchoredPosition = new Vector2(0, childRect.anchoredPosition.y);
            }
        }
    }
}