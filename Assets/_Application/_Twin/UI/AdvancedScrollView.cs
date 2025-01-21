using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI
{
    public class AdvancedScrollView : MonoBehaviour
    {
        private ScrollRect scrollRect;

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        private void Start()
        {
            ResetScrollActive();
        }

        private void OnRectTransformDimensionsChange()
        {
            ResetScrollActive();
        }
        
        public void ResetScrollActive()
        {
            if (gameObject.activeInHierarchy)
                StartCoroutine(ResetScrollActiveAtEndOfFrame());
        }

        private IEnumerator ResetScrollActiveAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame(); //wait for the end of the frame, because the layout needs to be updated for this calculation to work
            var fitsInViewport = scrollRect.viewport.rect.height < scrollRect.content.rect.height;
            scrollRect.movementType = fitsInViewport ? ScrollRect.MovementType.Elastic : ScrollRect.MovementType.Clamped;
        }
    }
}
