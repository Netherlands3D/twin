using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class AddLayerPanel : MonoBehaviour
    {
        private float targetHeight = 200; // current open height
        [SerializeField] private float defaultOpenHeight = 200; // if the user resizes the panel smaller than minResizeHeightThreshold, it will revert to opening to this height
        [SerializeField] private float minHeight = 16; //minimum height of the panel: height of the panel when closed
        [SerializeField] private float minResizeHeightThreshold = 50; // minimum height of the panel when dragging that will reset the open height to the default value

        [SerializeField] private float maxHeight = 300; //max height of the panel

        // [SerializeField] private float speed = 2500; 
        [SerializeField] private float smoothTime = 1f;
        private float currentVelocity = 0;
        private RectTransform rectTransform;
        private bool isOpen;
        private Coroutine activeAnimationCoroutine;
        [SerializeField] private Toggle toggle;
        private bool isDragging;

        public UnityEvent<float> OnRectTransformSizeChanged = new();

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            targetHeight = defaultOpenHeight;
        }

        public void TogglePanel(bool open)
        {
            if (activeAnimationCoroutine != null)
                StopCoroutine(activeAnimationCoroutine);

            if (isDragging)
                return;

            activeAnimationCoroutine = StartCoroutine(AnimatePanel(open));
            isOpen = open;
        }

        public void ResizePanel(float delta)
        {
            if (!isOpen)
                targetHeight = minHeight;

            if (activeAnimationCoroutine != null)
                StopCoroutine(activeAnimationCoroutine);

            targetHeight += delta / transform.lossyScale.y;
            targetHeight = Mathf.Clamp(targetHeight, minHeight, maxHeight);
            rectTransform.sizeDelta = new Vector2(0, targetHeight);
            isOpen = true;

            if (toggle)
                toggle.isOn = isOpen;

            isDragging = true;
            OnRectTransformSizeChanged.Invoke(targetHeight);
        }

        private IEnumerator AnimatePanel(bool open)
        {
            float elapsedTime = 0;
            while (true)
            {
                // var delta = speed * Time.deltaTime;
                elapsedTime += Time.deltaTime;
                if (open)
                {
                    var newY = Mathf.SmoothStep(rectTransform.sizeDelta.y, targetHeight, elapsedTime/smoothTime);
                    var newSizeDelta = new Vector2(0, newY);
                    if (elapsedTime > smoothTime)
                    {
                        rectTransform.sizeDelta = new Vector2(0, targetHeight);
                        OnRectTransformSizeChanged.Invoke(rectTransform.sizeDelta.y);
                        activeAnimationCoroutine = null;
                        yield break;
                    }

                    rectTransform.sizeDelta = newSizeDelta;
                    OnRectTransformSizeChanged.Invoke(rectTransform.sizeDelta.y);
                    yield return null;
                }
                else
                {
                    var newY = Mathf.SmoothStep(rectTransform.sizeDelta.y, minHeight, elapsedTime/smoothTime);
                    var newSizeDelta = new Vector2(0, newY);
                    if (elapsedTime > smoothTime)
                    {
                        rectTransform.sizeDelta = new Vector2(0, minHeight);
                        OnRectTransformSizeChanged.Invoke(rectTransform.sizeDelta.y);
                        activeAnimationCoroutine = null;
                        yield break;
                    }

                    rectTransform.sizeDelta = newSizeDelta;
                    OnRectTransformSizeChanged.Invoke(rectTransform.sizeDelta.y);
                    yield return null;
                }
            }
        }

        public void EndResizeAction()
        {
            if (targetHeight < minResizeHeightThreshold)
            {
                targetHeight = defaultOpenHeight;
                isOpen = false;

                if (toggle)
                    toggle.isOn = isOpen;
            }

            isDragging = false;
        }
    }
}