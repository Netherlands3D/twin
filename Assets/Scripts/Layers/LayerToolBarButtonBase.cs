using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Button))]
    public abstract class LayerToolBarButtonBase : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        protected LayerManager layerManager;
        protected Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            layerManager = GetComponentInParent<LayerManager>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(ButtonAction);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(ButtonAction);
        }

        public abstract void ButtonAction();
        public abstract void OnDrop(PointerEventData eventData);
        public void OnPointerEnter(PointerEventData eventData)
        {
            layerManager.MouseIsOverButton = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            layerManager.MouseIsOverButton = false;
        }
    }
}