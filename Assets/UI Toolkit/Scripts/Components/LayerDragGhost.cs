using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class LayerDragGhost : VisualElement
    {
        private Icon layerVisibilityImage;
        private VisualElement colorImage;
        private Icon foldoutImage;
        private VisualElement spacer;
        private Icon layerTypeImage;
        private Label layerNameText;

        private Vector2 currentPosition;
        private float yOffset;

        private VisualElement layerGhost;
        private VisualElement reorderLine;

        public LayerDragGhost()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            layerVisibilityImage = this.Q<Icon>("IsActiveIcon");
            colorImage = this.Q<VisualElement>("ColorBar");
            foldoutImage = this.Q<Icon>("FoldoutImage");
            spacer = this.Q<VisualElement>("Spacer");
            layerTypeImage = this.Q<Icon>("TypeIcon");
            layerNameText = this.Q<Label>("NameInputField");

            layerGhost = this.Q<VisualElement>("LayerGhost");
            reorderLine = this.Q<VisualElement>("ReorderLine");

            style.position = Position.Absolute;
            this.SetPickingModeRecursive(PickingMode.Ignore);
        }

        public void Initialize(Vector2 dragStartPosition, LayerListViewItem ui)
        {
            UpdatePosition(dragStartPosition);
            CopyAppearance(ui);
            yOffset = ui.resolvedStyle.height / 2;
        }

        private void CopyAppearance(LayerListViewItem ui)
        {
            layerVisibilityImage.Image = (IconImage)ui.VisibilityState;
            UpdateColorBar(ui.layerData.Color);
            var hasChildren = ui.layerData.ChildrenLayers.Count > 0;
            var indentWidth = ui.IndentWidth;

            if (!hasChildren)
                foldoutImage.Image = IconImage.None;

            spacer.style.width = indentWidth;
            layerTypeImage.Image = ui.LayerTypeIcon;
            layerNameText.text = ui.layerData.Name;

            // var credentialsUI = GetComponent<LayerUICredentialsNeededListener>();
            // credentialsUI.layerUI = ui;
        }

        private void UpdateColorBar(Color newColor)
        {
            var opaqueColor = newColor;
            opaqueColor.a = 1;

            colorImage.style.backgroundColor = opaqueColor;
        }

        public void UpdatePosition(Vector2 worldPosition)
        {
            layerGhost.style.top = parent.WorldToLocal(worldPosition).y - yOffset;
        }

        public void UpdateLine(LayerListViewItem targetItem, LayerPanel.DropMode currentDropMode)
        {
            var top = 0f;
            switch (currentDropMode)
            {
                case LayerPanel.DropMode.Above:
                {
                    reorderLine.style.display = DisplayStyle.Flex;
                    top = parent.WorldToLocal(new Vector2(0, targetItem.ItemRoot.worldBound.yMin)).y;
                    reorderLine.style.left = targetItem.ItemRoot.Q(className: "unity-tree-view__item-toggle").worldBound.xMin;
                    reorderLine.style.left = parent.WorldToLocal(targetItem.ItemRoot.Q(className: "unity-tree-view__item-toggle").worldBound).xMax;
                    break;
                }
                case LayerPanel.DropMode.Below:
                {
                    reorderLine.style.display = DisplayStyle.Flex;
                    top = parent.WorldToLocal(new Vector2(0, targetItem.ItemRoot.worldBound.yMax)).y;
                    reorderLine.style.left = parent.WorldToLocal(targetItem.ItemRoot.Q(className: "unity-tree-view__item-toggle").worldBound).xMax;
                    break;
                }
                case LayerPanel.DropMode.Into:
                {
                    reorderLine.style.display = DisplayStyle.None;
                    break;
                }
                case LayerPanel.DropMode.ToRoot:
                {
                    var targetLayer = targetItem.layerData;

                    reorderLine.style.display = DisplayStyle.Flex;
                    reorderLine.style.left = 0;

                    bool aboveFirst = targetLayer.ParentLayer.ChildrenLayers.IndexOf(targetLayer) == 0;
                    if (aboveFirst)
                        top = 0;
                    else
                        top = parent.WorldToLocal(new Vector2(targetItem.worldBound.xMax, targetItem.worldBound.yMax)).y;
                    
                    break;
                }
            }
            reorderLine.style.top = top - 1;
        }
    }
}