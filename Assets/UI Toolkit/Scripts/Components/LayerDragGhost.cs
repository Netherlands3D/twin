using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI;
using Netherlands3D.UI.ExtensionMethods;
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
            
            style.position = Position.Absolute;
            SetPickingModeRecursive(this, PickingMode.Ignore);
        }
        
        private void SetPickingModeRecursive(VisualElement element, PickingMode mode)
        {
            element.pickingMode = mode;
            foreach (var child in element.Children())
            {
                SetPickingModeRecursive(child, mode);
            }
        }

        public void Initialize(Vector2 dragStartPosition, LayerListViewItem ui)
        {
            currentPosition = dragStartPosition;
            ApplyPosition();
            CopyAppearance(ui);
        }

        private void CopyAppearance(LayerListViewItem ui)
        {
            layerVisibilityImage.Image = (IconImage)ui.VisibilityState;
            UpdateColorBar(ui.layerData.Color);
            var hasChildren = ui.layerData.ChildrenLayers.Count > 0;
            var indentWidth = ui.IndentWidth;
            
            if(!hasChildren)
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
        
        public void UpdatePosition(Vector2 delta)
        {
            currentPosition += delta;
            ApplyPosition();
        }

        private void ApplyPosition()
        {
            style.top = currentPosition.y;// - resolvedStyle.height / 2; //todo ui-toolkit: not the correct height offset yet
        }
    }
}