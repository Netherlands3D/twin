using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin
{
    public class ContextMenuUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public ContextMenuUI ParentMenu { get; set; }
        public ContextSubmenuFoldout FoldoutParent { get; set; }
        private static float horizontalOverlap = 3f;
        public static List<ContextMenuUI> ContextMenuUIs = new();
        public static bool OverAnyContextMenuUI
        {
            get
            {
                foreach (var contextMenuUI in ContextMenuUIs)
                {
                    if (contextMenuUI.MouseOver)
                        return true;
                }
                return false;
            }
        }

        public bool MouseOver { get; private set; } = false;

        private void Awake()
        {
            ContextMenuUIs.Add(this);
        }

        private void OnDestroy()
        {
            ContextMenuUIs.Remove(this);
        }

        public void RecalculatePosition(ContextSubmenuFoldout relativeTo)
        {
            FoldoutParent = relativeTo;
            ParentMenu = relativeTo.GetComponentInParent<ContextMenuUI>();
            var relativeRectTransform = relativeTo.transform as RectTransform;
            var scaledButtonSize = relativeRectTransform.rect.size * relativeRectTransform.lossyScale;
            var offset = CalculatePlaceToTheRight() ? new Vector3(scaledButtonSize.x / 2 - horizontalOverlap, scaledButtonSize.y / 2, 0) : new Vector3(-scaledButtonSize.x / 2 + horizontalOverlap - scaledButtonSize.x, scaledButtonSize.y / 2, 0);
            var newPosition = relativeTo.transform.position + offset;

            //todo: clamp position within screen bounds if needed
            
            transform.position = newPosition;

        }

        private bool CalculatePlaceToTheRight()
        {
            if (FoldoutParent)
            {
                var prt = FoldoutParent.transform as RectTransform;
                var leftPos = prt.position.x + (prt.rect.width * prt.lossyScale.x);
                var rt = transform as RectTransform;
                var width = rt.rect.width * rt.lossyScale.x;
                if (leftPos + width > Screen.width)
                    return false;
            }
            return true;
        }
        //
        // private void RecalculateSubmenuPosition()
        // {
        //     var scaledSize = rectTransform.rect.size * rectTransform.lossyScale;
        //     var offset = submenuAppearsToTheRight ? new Vector3(scaledSize.x / 2 - horizontalOverlap, scaledSize.y / 2, 0) : new Vector3(-scaledSize.x / 2 + horizontalOverlap, scaledSize.y / 2, 0);
        //     submenu.transform.position = transform.position + offset;
        // }
        public void OnPointerEnter(PointerEventData eventData)
        {
            MouseOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            MouseOver = false;
        }

        public void CloseBaseMenu()
        {
            var parentMenu = ParentMenu;
            while (parentMenu.ParentMenu)
            {
                if(ParentMenu.ParentMenu)
                    parentMenu = ParentMenu.ParentMenu;
            }
            print("base: " + parentMenu.gameObject.name);
            Destroy(parentMenu.gameObject);
        }
    }
}