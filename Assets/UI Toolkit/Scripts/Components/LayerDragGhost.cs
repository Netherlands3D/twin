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
        private VisualElement colorBar;
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
            colorBar = this.Q<VisualElement>("ColorBar");
            foldoutImage = this.Q<Icon>("FoldoutImage");
            spacer = this.Q<VisualElement>("Spacer");
            layerTypeImage = this.Q<Icon>("TypeIcon");
            layerNameText = this.Q<Label>("NameInputField");

            layerGhost = this.Q<VisualElement>("LayerGhost");
            reorderLine = this.Q<VisualElement>("ReorderLine");

            this.SetPickingModeRecursive(PickingMode.Ignore);
        }

        public void Initialize(Vector2 dragStartPosition, LayerTreeViewItem ui)
        {
            UpdatePosition(dragStartPosition);
            CopyAppearance(ui);
            yOffset = ui.resolvedStyle.height / 2;
        }

        private void CopyAppearance(LayerTreeViewItem ui)
        {
            var validCredentials = ui.LayerData.HasValidCredentials;
            
            layerVisibilityImage.Image = validCredentials ? (IconImage)ui.VisibilityState : IconImage.Warning;
            UpdateColorBar(validCredentials ? ui.LayerData.Color : null);
            var hasChildren = ui.LayerData.ChildrenLayers.Count > 0;
            var indentWidth = ui.IndentWidth;

            if (!hasChildren)
                foldoutImage.Image = IconImage.None;

            spacer.style.width = indentWidth;
            layerTypeImage.Image = ui.LayerTypeIcon;
            layerNameText.text = ui.LayerData.Name;

            EnableInClassList("credentials-needed", !validCredentials);
        }

        private void UpdateColorBar(Color? newColor)
        {
            if (!newColor.HasValue)
            {
                colorBar.style.backgroundColor = StyleKeyword.Null;
                return;
            }

            var opaqueColor = newColor.Value;
            opaqueColor.a = 1;

            colorBar.style.backgroundColor = opaqueColor;
        }

        public void UpdatePosition(Vector2 worldPosition)
        {
            layerGhost.style.top = parent.WorldToLocal(worldPosition).y - yOffset;
        }

        public void UpdateLine(LayerTreeViewItem targetItem, LayerPanel.DropMode currentDropMode)
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
                case LayerPanel.DropMode.ToRootAbove:
                {
                    reorderLine.style.display = DisplayStyle.Flex;
                    reorderLine.style.left = 0;
                    top = 0;
                    
                    break;
                }
                case LayerPanel.DropMode.ToRootBelow:
                {
                    reorderLine.style.display = DisplayStyle.Flex;
                    reorderLine.style.left = 0;
                    top = parent.WorldToLocal(new Vector2(targetItem.worldBound.xMax, targetItem.worldBound.yMax)).y;
                    
                    break;
                }
            }
            reorderLine.style.top = top - 1;
        }
    }
}