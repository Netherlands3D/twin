using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SLIDDES.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class LayerUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public LayerNL3DBase Layer { get; set; }

        private RectTransform rectTransform;
        private VerticalLayoutGroup verticalLayoutGroup;

        private LayerManager layerManager;

        [SerializeField] private RectTransform parentRowRectTransform;
        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private Button colorButton;
        [SerializeField] private Toggle foldoutToggle;
        [SerializeField] private Image layerTypeImage;
        [SerializeField] private TMP_Text layerNameText;
        [SerializeField] private TMP_InputField layerNameField;
        [SerializeField] private RectTransform childrenPanel;

        private LayerUI relativeToLayer;
        private bool draggingLayerShouldBePlacedBeforeOtherLayer;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            verticalLayoutGroup = GetComponent<VerticalLayoutGroup>();
            layerManager = GetComponentInParent<LayerManager>();
        }

        private void Start()
        {
            UpdateLayerUI();
        }

        public void UpdateLayerUI()
        {
            UpdateName();
            var maxWidth = transform.parent.GetComponent<RectTransform>().rect.width;
            RecalculateNameWidth(maxWidth);
        }

        private void UpdateName()
        {
            layerNameText.text = Layer.name;
            layerNameField.text = Layer.name;
        }

        private void RecalculateNameWidth(float maxWidth)
        {
            var layerNameFieldRectTransform = layerNameField.GetComponent<RectTransform>();
            var width = maxWidth;
            width -= layerNameFieldRectTransform.anchoredPosition.x;
            width -= verticalLayoutGroup.spacing;
            width -= parentRowRectTransform.GetComponent<HorizontalLayoutGroup>().padding.right;
            layerNameFieldRectTransform.sizeDelta = new Vector2(width, layerNameFieldRectTransform.rect.height);
            layerNameText.GetComponent<RectTransform>().sizeDelta = layerNameFieldRectTransform.sizeDelta;
        }

        private void RecalculateChildPanelHeight()
        {
            var layerHeight = GetComponent<RectTransform>().rect.height;
            childrenPanel.SetHeight(Layer.Children.Count * layerHeight);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            layerManager.DragStartOffset = (Vector2)transform.position - eventData.position;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            layerManager.StartDragLayer(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (layerManager.DragLine.gameObject.activeInHierarchy)
                ReorderLayers(LayerManager.DraggingLayer, relativeToLayer, draggingLayerShouldBePlacedBeforeOtherLayer);
            else
                print("do reparent");

            layerManager.EndDraglayer();
        }

        // public GameObject test;

        public void OnDrag(PointerEventData eventData) //has to be here or OnBeginDrag and OnEndDrag won't work
        {
            relativeToLayer = CalculcateOverLayer();
            if (relativeToLayer)
            {
                var mousePos = Pointer.current.position.ReadValue();
                var hoverTransform = relativeToLayer.rectTransform; // as RectTransform;

                //25% above and below the centerline should reparent, the outer quarters should rearrange
                var minReparentThreshold = hoverTransform.rect.size.y / 4;
                var maxReparentThreshold = hoverTransform.rect.size.y / 4 * 3;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(hoverTransform, mousePos, null, out var localPoint);
                localPoint.y *= -1;
                // print(localPoint.y + "\t" + minReparentThreshold + "\t" + maxReparentThreshold);
                if (localPoint.y < minReparentThreshold)
                {
                    // print("higher");
                    draggingLayerShouldBePlacedBeforeOtherLayer = true;
                    layerManager.DragLine.gameObject.SetActive(true);
                    layerManager.DragLine.position = relativeToLayer.transform.position;
                    // ReorderLayers(LayerManager.OverLayer, false);
                }
                else if (localPoint.y > maxReparentThreshold)
                {
                    // print("lower");
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(true);
                    var correctedSize = hoverTransform.rect.size * hoverTransform.lossyScale;
                    layerManager.DragLine.position = relativeToLayer.transform.position - new Vector3(0, correctedSize.y, 0);
                    // ReorderLayers(LayerManager.OverLayer, true);
                }
                else
                {
                    layerManager.DragLine.gameObject.SetActive(false);
                    print("reparent");
                }
            }
        }

        private LayerUI CalculcateOverLayer()
        {
            var mousePos = Pointer.current.position.ReadValue();
            for (var i = 0; i < LayerManager.LayersVisibleInInspector.Count; i++)
            {
                var layer = LayerManager.LayersVisibleInInspector[i];
                var correctedSize = layer.rectTransform.rect.size * layer.rectTransform.lossyScale;
                var mouseInRect = RectTransformUtility.ScreenPointToLocalPointInRectangle(layer.rectTransform, mousePos, null, out var localPoint);
                if (i == 0 && localPoint.y > 0)
                {
                    return layer; //above first
                }

                if (localPoint.y >= 0 && localPoint.y < layer.rectTransform.rect.size.y)
                {
                    return layer;
                }
            }

            //below last
            return LayerManager.LayersVisibleInInspector.Last();
        }

        private void ReorderLayers(LayerUI changingLayer, LayerUI relativeTo, bool placeChangingLayerBefore)
        {
            if (changingLayer == relativeTo)
                return;

            LayerManager.LayersVisibleInInspector.Remove(changingLayer);

            var relativeToIndex = LayerManager.LayersVisibleInInspector.IndexOf(relativeTo);
            var newIndex = placeChangingLayerBefore ? relativeToIndex : relativeToIndex + 1;
            print(placeChangingLayerBefore + "\t" + newIndex);
            LayerManager.LayersVisibleInInspector.Insert(newIndex, changingLayer);
            LayerManager.DraggingLayer.transform.SetSiblingIndex(newIndex);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // layerManager.OnLayerEnter(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}