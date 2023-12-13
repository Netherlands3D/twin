using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Toggle))]
    public abstract class LayerToolBarToggleBase : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        protected LayerManager layerManager;
        protected Toggle toggle;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            layerManager = GetComponentInParent<LayerManager>();
        }

        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(ToggleAction);
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(ToggleAction);
        }

        public abstract void ToggleAction(bool isOn);
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