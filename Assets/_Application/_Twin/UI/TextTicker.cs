using System.Collections;
using UnityEngine;

namespace Netherlands3D.Twin.UI
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
            SetInitialPosition();
        }

        private void SetInitialPosition()
        {
            childWidth = childRect.rect.width;
            parentWidth = parentRect.rect.width;

            if (childWidth > parentWidth)
            {
                childRect.anchoredPosition = new Vector2(0, childRect.anchoredPosition.y);
            }
        }

  

        public void CheckAndStartScrolling()
        {
            childWidth = childRect.rect.width;
            parentWidth = parentRect.rect.width;

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

        public void StopScrolling()
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