using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class AddLayerPanel : MonoBehaviour
    {
        [SerializeField] private float openHeight = 500;
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

        private IEnumerator AnimatePanel(bool open)
        {
            while (true)
            {
                var delta = speed * Time.deltaTime;
                print(delta);
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
                        rectTransform.sizeDelta = new Vector2(0, 0);
                        activeAnimationCoroutine = null;
                        yield break;
                    }

                    rectTransform.sizeDelta = newSizeDelta;
                    yield return null;
                }
            }
        }
    }
}