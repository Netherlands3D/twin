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
        private Icon layerTypeImage;
        private Label layerNameText;
        
        private Vector2 currentPosition;
        
        public LayerDragGhost()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            colorImage = this.Q<VisualElement>("ColorBar");
            layerNameText = this.Q<Label>("NameInputField");
        }

        public void Initialize(Vector2 dragStartPosition, LayerListViewItem ui)
        {
            currentPosition = dragStartPosition;
            ApplyPosition();
            CopyAppearance(ui);
        }

        private void CopyAppearance(LayerListViewItem ui)
        {
            // layerVisibilityImage.Image = ui.VisibilitySprite;
            UpdateColorBar(ui.layerData.Color);
            // foldoutImage.enabled = ui.hasChildren;
            // layerTypeImage.sprite = ui.LayerTypeSprite;
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
            style.left = currentPosition.x;
            style.top = currentPosition.y;
        }
    }
}