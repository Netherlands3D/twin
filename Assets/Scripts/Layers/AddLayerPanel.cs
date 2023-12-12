using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class AddLayerPanel : MonoBehaviour
    {
        private float openHeight = 200; // current open height
        [SerializeField] private float defaultOpenHeight = 200; // if the user resizes the panel smaller than minResizeHeightThreshold, it will revert to opening to this height
        [SerializeField] private float minHeight = 16; //minimum height of the panel: height of the panel when closed
        [SerializeField] private float minResizeHeightThreshold = 50; // minimum height of the panel when dragging that will reset the open height to the default value
        [SerializeField] private float maxHeight = 300; //max height of the panel
        [SerializeField] private float speed = 1000; 
        private RectTransform rectTransform;
        private bool isOpen;
        private Coroutine activeAnimationCoroutine;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void TogglePanel()
        {
            TogglePanel(!isOpen);
        }

        void TogglePanel(bool open)
        {
            if (activeAnimationCoroutine != null)
                StopCoroutine(activeAnimationCoroutine);

            activeAnimationCoroutine = StartCoroutine(AnimatePanel(open));

            isOpen = open;
        }

        public void ResizePanel(float delta)
        {
            if (!isOpen)
                openHeight = minHeight;

            print(delta);
            if (activeAnimationCoroutine != null)
                StopCoroutine(activeAnimationCoroutine);

            openHeight += delta / transform.lossyScale.y;
            openHeight = Mathf.Clamp(openHeight, minHeight, maxHeight);
            rectTransform.sizeDelta = new Vector2(0, openHeight);
            isOpen = true;
        }

        private IEnumerator AnimatePanel(bool open)
        {
            while (true)
            {
                var delta = speed * Time.deltaTime;
                if (open)
                {
                    var newSizeDelta = new Vector2(0, rectTransform.sizeDelta.y + delta);
                    if (newSizeDelta.y > openHeight)
                    {
                        rectTransform.sizeDelta = new Vector2(0, openHeight);
                        activeAnimationCoroutine = null;
                        yield break;
                    }

                    rectTransform.sizeDelta = newSizeDelta;
                    yield return null;
                }
                else
                {
                    var newSizeDelta = new Vector2(0, rectTransform.sizeDelta.y - delta);
                    if (newSizeDelta.y < 0)
                    {
                        rectTransform.sizeDelta = new Vector2(0, minHeight);
                        activeAnimationCoroutine = null;
                        yield break;
                    }

                    rectTransform.sizeDelta = newSizeDelta;
                    yield return null;
                }
            }
        }

        public void RecalculateTargetHeight()
        {
            if (openHeight < minResizeHeightThreshold)
            {
                openHeight = defaultOpenHeight;
                isOpen = false;
            }
        }
    }
}