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
    public class LayerUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public LayerNL3DBase Layer { get; set; }

        private RectTransform rectTransform;
        private VerticalLayoutGroup verticalLayoutGroup;

        private LayerManager layerManager;

        [SerializeField] private RectTransform parentRowRectTransform;
        [SerializeField] private Toggle enabledToggle;
        [SerializeField] private Button colorButton;
        [SerializeField] private RectTransform spacer;
        [SerializeField] private float indentWidth = 30f;
        [SerializeField] private Toggle foldoutToggle;
        [SerializeField] private Image layerTypeImage;
        [SerializeField] private TMP_Text layerNameText;
        [SerializeField] private TMP_InputField layerNameField;
        [SerializeField] private RectTransform childrenPanel;

        private LayerUI layerUnderMouse;
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
            UpdateUIParent();

            UpdateName();
            var maxWidth = transform.parent.GetComponent<RectTransform>().rect.width;
            RecalculateNameWidth(maxWidth);
        }

        private void UpdateUIParent()
        {
            if (!Layer.Parent && transform.parent != layerManager.transform)
            {
                transform.SetParent(layerManager.transform);
                RecalculateLayersVisibleInHierarchyAfterReparent(this, transform.GetSiblingIndex());
            }
            else if (Layer.Parent && Layer.Parent.UI.childrenPanel != transform.parent)
            {
                transform.SetParent(Layer.Parent.UI.childrenPanel);
                RecalculateLayersVisibleInHierarchyAfterReparent(this, transform.GetSiblingIndex());
            }

            RecalculateIndent();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layerManager.transform as RectTransform); //not sure why it is needed to manually force a canvas update
        }

        private void RecalculateLayersVisibleInHierarchyAfterReparent(LayerUI changingLayer, int newSiblingIndex)
        {
            LayerManager.LayersVisibleInInspector.Remove(changingLayer);

            // var parentIndex = 0;
            // if (changingLayer.Layer.Parent)
            //     parentIndex += LayerManager.LayersVisibleInInspector.IndexOf(changingLayer.Layer.Parent.UI);


            var relativeToIndex = LayerManager.LayersVisibleInInspector.IndexOf(layerUnderMouse);

            var newVisibleIndex = newSiblingIndex + relativeToIndex + 1;
            // var newVisibleIndex = draggingLayerShouldBePlacedBeforeOtherLayer ? relativeToIndex : relativeToIndex + 1;
            print(changingLayer.Layer.name + "\t" + layerUnderMouse.Layer.name + "\t" + draggingLayerShouldBePlacedBeforeOtherLayer + "\t" + newVisibleIndex);
            LayerManager.LayersVisibleInInspector.Insert(newVisibleIndex, changingLayer);
            // LayerManager.DraggingLayer.transform.SetSiblingIndex(newSiblingIndex);

            // var newIndex = parentIndex + changingLayer.transform.GetSiblingIndex() + 1; //draggingLayerShouldBePlacedBeforeOtherLayer ? relativeToIndex : relativeToIndex + 1;
            // print("Visible index of dragged layer after reparent\t" + newIndex);
            // LayerManager.LayersVisibleInInspector.Insert(newIndex, changingLayer);
            // LayerManager.DraggingLayer.transform.SetSiblingIndex(newIndex);

            foreach (var child in changingLayer.Layer.Children) //recursively recalculate indices of all changed children
            {
                RecalculateLayersVisibleInHierarchyAfterReparent(child.UI, child.UI.transform.GetSiblingIndex());
            }

            string s = changingLayer.Layer.name;
            for (var index = 0; index < LayerManager.LayersVisibleInInspector.Count; index++)
            {
                var l = LayerManager.LayersVisibleInInspector[index];
                s += " - " + index + l.Layer.name;
            }

            print(s);
        }

        private void RecalculateIndent()
        {
            var spacerRectTransform = spacer.transform as RectTransform;
            spacerRectTransform.sizeDelta = new Vector2(Layer.Depth * indentWidth, spacerRectTransform.sizeDelta.y);
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
                ReorderLayers(LayerManager.DraggingLayer, layerUnderMouse, draggingLayerShouldBePlacedBeforeOtherLayer);
            else
                LayerManager.DraggingLayer.Layer.SetParent(layerUnderMouse.Layer);

            layerManager.EndDraglayer();
        }

        public void OnDrag(PointerEventData eventData) //has to be here or OnBeginDrag and OnEndDrag won't work
        {
            layerUnderMouse = CalculcateLayerUnderMouse(out float relativeYValue);
            print(Layer.name + "\t" + layerUnderMouse.Layer.name);
            if (layerUnderMouse)
            {
                var hoverTransform = layerUnderMouse.rectTransform; // as RectTransform;
                var correctedSize2 = hoverTransform.rect.size * hoverTransform.lossyScale;
                var yValue01 = Mathf.Clamp01(-relativeYValue / correctedSize2.y);
                // print(relativeYValue + "\t" + yValue01);

                if (yValue01 < 0.25f)
                {
                    // print("higher");
                    draggingLayerShouldBePlacedBeforeOtherLayer = true;
                    layerManager.DragLine.gameObject.SetActive(true);
                    layerManager.DragLine.position = layerUnderMouse.transform.position;
                    // ReorderLayers(LayerManager.OverLayer, false);
                }
                else if (yValue01 > 0.75f)
                {
                    // print("lower");
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(true);
                    var correctedSize = hoverTransform.rect.size * hoverTransform.lossyScale;
                    layerManager.DragLine.position = layerUnderMouse.transform.position - new Vector3(0, correctedSize.y, 0);
                    // ReorderLayers(LayerManager.OverLayer, true);
                }
                else
                {
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(false);
                }
            }
        }

        private LayerUI CalculcateLayerUnderMouse(out float relativeYValue)
        {
            var mousePos = Pointer.current.position.ReadValue();
            for (var i = LayerManager.LayersVisibleInInspector.Count - 1; i >= 0; i--)
            {
                var layer = LayerManager.LayersVisibleInInspector[i];
                var correctedSize = layer.rectTransform.rect.size * layer.rectTransform.lossyScale;
                if (mousePos.y < layer.rectTransform.position.y && mousePos.y >= layer.rectTransform.position.y - correctedSize.y)
                {
                    print("between " + layer.Layer.name);
                    relativeYValue = mousePos.y - layer.rectTransform.position.y;
                    return layer;
                }
            }

            var firstLayer = LayerManager.LayersVisibleInInspector[0];
            if (mousePos.y >= firstLayer.rectTransform.position.y)
            {
                print("above first");
                relativeYValue = mousePos.y - firstLayer.rectTransform.position.y;
                return firstLayer; //above first
            }

            print("below last");
            var lastLayer = LayerManager.LayersVisibleInInspector.Last();
            relativeYValue = mousePos.y - lastLayer.rectTransform.position.y;
            return lastLayer; //below last


            // for (var i = 0; i < LayerManager.LayersVisibleInInspector.Count; i++)
            // {
            //     var layer = LayerManager.LayersVisibleInInspector[i];
            //     var correctedSize = layer.rectTransform.rect.size * layer.rectTransform.lossyScale;
            //     var mouseInRect = RectTransformUtility.ScreenPointToLocalPointInRectangle(layer.rectTransform, mousePos, null, out var localPoint);
            //
            //     print(layer.Layer.name + "\t" + layer.rectTransform.transform.position.y + "\t" + mousePos.y);
            //
            //     if (i == 0 && localPoint.y > 0)
            //     {
            //         print("above first");
            //         return layer; //above first
            //     }
            //
            //     if (localPoint.y >= 0 && localPoint.y < layer.rectTransform.rect.size.y)
            //     {
            //         print(i + " mouse in rect: " + mouseInRect + "\t" + layer.Layer.name);
            //         return layer;
            //     }
            // }
            //
            // //below last
            // print("below last");
            // return LayerManager.LayersVisibleInInspector.Last();
        }

        private void ReorderLayers(LayerUI changingLayer, LayerUI relativeTo, bool placeChangingLayerBefore)
        {
            if (changingLayer == relativeTo)
                return;

            // LayerManager.LayersVisibleInInspector.Remove(changingLayer);

            //check if reorder includes reparent
            var newChildIndex = placeChangingLayerBefore ? relativeTo.transform.GetSiblingIndex() : relativeTo.transform.GetSiblingIndex() + 1;
            changingLayer.Layer.SetParent(relativeTo.Layer.Parent, newChildIndex);

            // var relativeToIndex = LayerManager.LayersVisibleInInspector.IndexOf(relativeTo);
            // var newIndex = placeChangingLayerBefore ? relativeToIndex : relativeToIndex + 1;
            //
            // print(placeChangingLayerBefore + "\t" + newIndex);
            // LayerManager.LayersVisibleInInspector.Insert(newIndex, changingLayer);
            // LayerManager.DraggingLayer.transform.SetSiblingIndex(newIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
        }
    }
}