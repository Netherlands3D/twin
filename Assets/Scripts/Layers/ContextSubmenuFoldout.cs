using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class ContextSubmenuFoldout : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static float appearTime = 0.3f;
        private Coroutine timerCoroutine;

        [SerializeField] private ContextMenuUI submenuPrefab;
        private ContextMenuUI submenu;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!submenu)
                timerCoroutine = StartCoroutine(ShowSubmenuAfterDelay());
        }

        private IEnumerator ShowSubmenuAfterDelay()
        {
            yield return new WaitForSecondsRealtime(appearTime);
            submenu = Instantiate(submenuPrefab, GetComponentInParent<LayerUIManager>().transform);
            submenu.RecalculatePosition(this);
            timerCoroutine = null;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (timerCoroutine != null)
                StopCoroutine(timerCoroutine);

            if (submenu)
            {
                var submenuRect = submenu.transform as RectTransform;
                var point = submenuRect.InverseTransformPoint(eventData.position);

                if (!submenuRect.rect.Contains(point))
                    Destroy(submenu.gameObject);
            }
        }

        private void OnDestroy()
        {
            if (submenu)
                Destroy(submenu.gameObject);
        }
    }
}