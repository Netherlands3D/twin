using System;
using System.Linq;
using SLIDDES.UI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public enum InteractionState
    {
        Default,
        Hover,
        Selected
    }

    public class LayerUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public LayerNL3DBase Layer { get; set; }

        private RectTransform rectTransform;
        private VerticalLayoutGroup verticalLayoutGroup;

        private LayerManager layerManager;
        public Transform LayerBaseTransform => layerManager.transform;

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

        [SerializeField] private TMP_Text debugIndexText;

        private LayerUI parentUI;
        private LayerUI[] childrenUI = Array.Empty<LayerUI>();

        private LayerUI layerUnderMouse;
        private LayerUI reparentLayer;
        private bool draggingLayerShouldBePlacedBeforeOtherLayer;

        public Color Color { get; set; } = Color.blue;
        public Sprite Icon { get; set; }
        public int Depth { get; private set; } = 0;

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

        public void SetParent(LayerUI newParent, int siblingIndex = -1)
        {
            // if (newParent)
            //     print("reparenting " + Layer.name + " to " + newParent.Layer.name);
            // else
            //     print("reparenting " + Layer.name + " to null");

            var oldParent = parentUI;
            // if (oldParent == null)
            // {
            //     LayerManager.UnparentedLayers.Remove(this);
            // }

            if (newParent == null)
            {
                transform.SetParent(LayerBaseTransform);
                // LayerManager.UnparentedLayers.Add(this);
                // LayerManager.UnparentedLayers.Sor((ui)=>ui.transform.GetSiblingIndex());
            }
            else
            {
                transform.SetParent(newParent.childrenPanel);
            }


            // if (newParent && childIndex >= 0 && childIndex < newParent.transform.childCount)
            // {
            // print("setting child index to: " + siblingIndex);
            transform.SetSiblingIndex(siblingIndex);
            // }
            // if(newParent ==null && childIndex >=0 )


            if (oldParent)
                oldParent.RecalculateParentAndChildren();
            if (newParent)
                newParent.RecalculateParentAndChildren();
            RecalculateParentAndChildren();

            RecalculateDepthValuesRecursively();
            // RecalculateLayersVisibleInHierarchyAfterReparent(this, newParent, transform.GetSiblingIndex());
            RecalculateVisibleHierarchyRecursive();

            // Canvas.ForceUpdateCanvases();
            // LayoutRebuilder.MarkLayoutForRebuild(layerManager.transform as RectTransform); //not sure why it is needed to manually force a canvas update
        }

        private void RecalculateVisibleHierarchyRecursive()
        {
            LayerManager.LayersVisibleInInspector.Clear();
            foreach (Transform unparentedLayer in LayerBaseTransform)
            {
                var ui = unparentedLayer.GetComponent<LayerUI>();
                // print("recalculating unparented layer: " + ui.Layer.name);
                ui.RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers();
            }
        }

        private void RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers()
        {
            // print("adding " + Layer.name + " to list at index " + LayerManager.LayersVisibleInInspector.Count);
            LayerManager.LayersVisibleInInspector.Add(this);
            if (foldoutToggle.isOn)
            {
                // print("parent " + Layer.name + " is enabled");
                foreach (var child in childrenUI)
                {
                    // print("child: " + child.Layer.name);
                    child.RecalculateLayersVisibleInHierarchyRecursiveForParentedLayers();
                }
            }

            UpdateLayerUI();
        }

        private void RecalculateParentAndChildren()
        {
            parentUI = transform.parent.GetComponentInParent<LayerUI>(); // use transform.parent.GetComponentInParent to avoid getting the LayerUI on this gameObject
            childrenUI = childrenPanel.GetComponentsInChildren<LayerUI>();
            // if (parentUI)
            //     print(Layer.name + " has parent " + parentUI.Layer.name + " and " + childrenUI.Length + " children");
            // else
            //     print(Layer.name + " has no parent and " + childrenUI.Length + " children");
        }


        private void RecalculateDepthValuesRecursively()
        {
            if (transform.parent != LayerBaseTransform)
                Depth = parentUI.Depth + 1;
            else
                Depth = 0;

            foreach (var child in childrenUI)
            {
                child.RecalculateDepthValuesRecursively();
            }
            // UpdateLayerUI();
        }

        public void UpdateLayerUI()
        {
            // print("updating ui of: " + Layer.name);

            UpdateName();
            var maxWidth = transform.parent.GetComponent<RectTransform>().rect.width;
            RecalculateNameWidth(maxWidth);
            RecalculateIndent();
            debugIndexText.text = "Vi: " + LayerManager.LayersVisibleInInspector.IndexOf(this) + "\nSi: " + transform.GetSiblingIndex();
            // print("Vi: " + LayerManager.LayersVisibleInInspector.IndexOf(this) + "\nSi: " + transform.GetSiblingIndex());
            Canvas.ForceUpdateCanvases();
            // LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform); //not sure why it is needed to manually force a canvas update
            // LayoutRebuilder.MarkLayoutForRebuild(childrenPanel as RectTransform); //not sure why it is needed to manually force a canvas update
            // LayoutRebuilder.ForceRebuildLayoutImmediate(layerManager.transform as RectTransform); //not sure why it is needed to manually force a canvas update
        }

        private void RecalculateLayersVisibleInHierarchyAfterReparent(LayerUI changingLayer, LayerUI newParent, int newSiblingIndex)
        {
            LayerManager.LayersVisibleInInspector.Remove(changingLayer);
            var newIndex = 0;
            if (newParent)
                newIndex += LayerManager.LayersVisibleInInspector.IndexOf(newParent) + 1;

            print("new parentIndex" + newIndex);
            print("new sibling index " + newSiblingIndex);
            newIndex += newSiblingIndex;
            print("new total index " + newIndex);

            if (newIndex < LayerManager.LayersVisibleInInspector.Count)
                LayerManager.LayersVisibleInInspector.Insert(newIndex, changingLayer);
            else
                LayerManager.LayersVisibleInInspector.Add(changingLayer);

            string st = changingLayer.Layer.name;
            for (var index = 0; index < LayerManager.LayersVisibleInInspector.Count; index++)
            {
                var l = LayerManager.LayersVisibleInInspector[index];
                st += " - " + index + l.Layer.name;
            }

            print(st);

            return;

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

            // foreach (Transform child in transform) //recursively recalculate indices of all changed children
            // {
            //     RecalculateLayersVisibleInHierarchyAfterReparent(child.GetComponent<LayerUI>(), child.GetSiblingIndex());
            // }

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
            spacerRectTransform.sizeDelta = new Vector2(Depth * indentWidth, spacerRectTransform.sizeDelta.y);
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
            childrenPanel.SetHeight(transform.childCount * layerHeight);
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
            if (LayerManager.DraggingLayer == layerUnderMouse)
            {
                layerManager.EndDraglayer();
                return;
            }

            if (layerManager.DragLine.gameObject.activeInHierarchy)
            {
                var newParent = layerUnderMouse.parentUI;
                var newSiblingIndex = draggingLayerShouldBePlacedBeforeOtherLayer ? layerUnderMouse.transform.GetSiblingIndex() : layerUnderMouse.transform.GetSiblingIndex() + 1;
                //edge case: if the reorder is between layerUnderMouse, and between layerUnderMouse and child 0 of layerUnderMouse, the new parent should be the layerUnderMouse instead of the layerUnderMouse's parent 
                if (!draggingLayerShouldBePlacedBeforeOtherLayer && layerUnderMouse.childrenUI.Length > 0 && layerUnderMouse.foldoutToggle.isOn) 
                {
                    newParent = layerUnderMouse;
                    newSiblingIndex = 0;
                }

                // print("reorder: before: " + draggingLayerShouldBePlacedBeforeOtherLayer + " layer: " + layerUnderMouse.Layer.name + "new parent" + layerUnderMouse.parentUI.Layer.name);
                print("reorder: before: " + draggingLayerShouldBePlacedBeforeOtherLayer + "\t" + newSiblingIndex);
                SetParent(newParent, newSiblingIndex);
            }
            else
            {
                print("reparent " + Layer.name + "to :" + layerUnderMouse.Layer.name + "at index " + layerUnderMouse.childrenPanel.childCount);
                SetParent(layerUnderMouse, layerUnderMouse.childrenPanel.childCount);
            }
            
            if (layerUnderMouse)
                layerUnderMouse.SetHighlight(layerUnderMouse, InteractionState.Default);
            
            layerManager.EndDraglayer();
        }

        public void OnDrag(PointerEventData eventData) //has to be here or OnBeginDrag and OnEndDrag won't work
        {
            if (layerUnderMouse)
                layerUnderMouse.SetHighlight(layerUnderMouse, InteractionState.Default);
            var layerAndIndex = CalculateLayerUnderMouse(out float relativeYValue);
            layerUnderMouse = layerAndIndex.Item1;
            layerUnderMouse.SetHighlight(layerUnderMouse, InteractionState.Hover);
            // print(Layer.name + "\t" + layerUnderMouse.Layer.name);
            if (layerUnderMouse)
            {
                var hoverTransform = layerUnderMouse.rectTransform; // as RectTransform;
                var correctedSize2 = hoverTransform.rect.size * hoverTransform.lossyScale;
                var yValue01 = Mathf.Clamp01(-relativeYValue / correctedSize2.y);

                //todo: if mouse is fully to the bottom, set parent to null

                // print(relativeYValue + "\t" + yValue01);

                //todo: calculate reparent layer and sibling index here instead of in OnEndDrag
                if (yValue01 < 0.25f)
                {
                    print("higher");
                    draggingLayerShouldBePlacedBeforeOtherLayer = true;
                    layerManager.DragLine.gameObject.SetActive(true);
                    layerManager.DragLine.position = new Vector2(layerManager.DragLine.position.x, layerUnderMouse.transform.position.y);
                    // ReorderLayers(LayerManager.OverLayer, false);
                }
                else if (yValue01 > 0.75f)
                {
                    print("lower");
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(true);
                    var correctedSize = hoverTransform.rect.size * hoverTransform.lossyScale;
                    layerManager.DragLine.position = new Vector2(layerManager.DragLine.position.x, layerUnderMouse.transform.position.y) - new Vector2(0, correctedSize.y);
                    // ReorderLayers(LayerManager.OverLayer, true);
                }
                else
                {
                    print("reparent");
                    draggingLayerShouldBePlacedBeforeOtherLayer = false;
                    layerManager.DragLine.gameObject.SetActive(false);
                }
            }
        }

        private void SetHighlight(LayerUI layer, InteractionState state)
        {
            switch (state)
            {
                case InteractionState.Default:
                    layer.GetComponentInChildren<Image>().color = Color.red;
                    break;
                case InteractionState.Hover:
                    layer.GetComponentInChildren<Image>().color = Color.cyan;
                    break;
                case InteractionState.Selected:
                    layer.GetComponentInChildren<Image>().color = Color.blue;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private (LayerUI, int) CalculateLayerUnderMouse(out float relativeYValue)
        {
            // var mousePos = Pointer.current.position.ReadValue();
            var ghostRectTransform = layerManager.DragGhost.GetComponent<RectTransform>();
            var ghostSize = ghostRectTransform.rect.size * ghostRectTransform.lossyScale;
            var mousePos = (Vector2)layerManager.DragGhost.transform.position - ghostSize / 2;

            for (var i = LayerManager.LayersVisibleInInspector.Count - 1; i >= 0; i--)
            {
                var layer = LayerManager.LayersVisibleInInspector[i];
                var correctedSize = layer.rectTransform.rect.size * layer.rectTransform.lossyScale;
                if (mousePos.y < layer.rectTransform.position.y && mousePos.y >= layer.rectTransform.position.y - correctedSize.y)
                {
                    // print("between " + layer.Layer.name);
                    relativeYValue = mousePos.y - layer.rectTransform.position.y;
                    return (layer, i);
                }
            }

            var firstLayer = LayerManager.LayersVisibleInInspector[0];
            if (mousePos.y >= firstLayer.rectTransform.position.y)
            {
                // print("above first");
                relativeYValue = mousePos.y - firstLayer.rectTransform.position.y;
                return (firstLayer, 0); //above first
            }

            // print("below last");
            var lastLayer = LayerManager.LayersVisibleInInspector.Last();
            relativeYValue = mousePos.y - lastLayer.rectTransform.position.y;
            return (lastLayer, LayerManager.LayersVisibleInInspector.Count - 1); //below last
        }

        public void OnPointerClick(PointerEventData eventData)
        {
        }
    }
}