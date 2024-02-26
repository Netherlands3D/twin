using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Toggle))]
    public class LayerToggle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField]
        protected Transform layerParent;
        protected Toggle toggle;

        [SerializeField]
        protected ReferencedLayer layer;

        [SerializeField]
        protected GameObject prefab;

        [SerializeField]
        protected GameObject binImage;

        [SerializeField]
        protected Sprite hoverSprite;
        protected Sprite defaultSprite;

        protected virtual void Awake()
        {
            toggle = GetComponent<Toggle>();
            defaultSprite = GetComponent<Image>().sprite;
        }

        protected virtual void OnEnable()
        {
            toggle.isOn = layer != null;
            ShowBin(false);

            LayerData.LayerDeleted.AddListener(OnLayerDeleted);
            toggle.onValueChanged.AddListener(CreateOrDestroyObject);
        }

        protected virtual void OnDisable()
        {
            LayerData.LayerDeleted.RemoveListener(OnLayerDeleted);
            toggle.onValueChanged.RemoveListener(CreateOrDestroyObject);
        }

        private void OnLayerDeleted(LayerNL3DBase deletedLayer)
        {
            if (!toggle.isOn)
                return;

            if (deletedLayer == layer.ReferencedProxy)
            {
                toggle.onValueChanged.RemoveListener(CreateOrDestroyObject); //the layer was already deleted, it should only update the toggle
                toggle.isOn = false; //use the regular way instead of SetIsOnWithoutNotify because the toggle graphics should update.
                toggle.onValueChanged.AddListener(CreateOrDestroyObject);
            }
        }

        private void CreateOrDestroyObject(bool isOn)
        {
            if (isOn)
                layer = CreateObject();
            else
                layer.DestroyLayer();
        }

        private ReferencedLayer CreateObject()
        {
            var newObject = Instantiate(prefab, Vector3.zero, Quaternion.identity, layerParent);
            newObject.name = prefab.name;

            var layerComponent = newObject.GetComponent<ReferencedLayer>();
            if (!layerComponent)
                layerComponent = newObject.AddComponent<ReferencedLayer>();

            StartCoroutine(SelectAndHoverAtEndOfFrame()); //wait until layer and UI are initialized.

            return layerComponent;
        }

        private IEnumerator SelectAndHoverAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            layer.ReferencedProxy.UI.Select();
            HighlightLayer(true);
            layer.ReferencedProxy.name = prefab.name;
            layer.ReferencedProxy.UI.MarkLayerUIAsDirty();
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            ShowBin(toggle.isOn);
            GetComponent<Image>().sprite = hoverSprite;
            HighlightLayer(true);
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            ShowBin(false);
            GetComponent<Image>().sprite = defaultSprite;
            HighlightLayer(false);
        }

        //also called in the inspector to update after a press
        public void ShowBin(bool isOn)
        {
            binImage.SetActive(isOn);
        }

        private void HighlightLayer(bool isOn)
        {
            if (!layer || !layer.ReferencedProxy || !layer.ReferencedProxy.UI)
                return;

            var layerState = isOn ? InteractionState.Hover : InteractionState.Default;
            layer.ReferencedProxy.UI.SetHighlight(layerState);
        }
    }
}
