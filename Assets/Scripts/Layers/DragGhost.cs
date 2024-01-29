using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class DragGhost : MonoBehaviour//, IPointerDownHandler, IPointerUpHandler
    {
        private Vector2 DragStartOffset { get; set; }
        
        [SerializeField] private Image layerVisibilityImage;
        [SerializeField] private Image colorImage;
        [SerializeField] private Image foldoutImage;
        [SerializeField] private Image layerTypeImage;
        [SerializeField] private TMP_Text layerNameText;

        public void Initialize(Vector2 dragStartOffset, LayerUI ui)
        {
            // transform.position = startPosition;
            // var pointerPosition = Pointer.current.position.ReadValue();
            DragStartOffset = dragStartOffset;
            CalculateNewPosition();
            
            CopyAppearance(ui);
        }

        private void CopyAppearance(LayerUI ui)
        {
            layerVisibilityImage.sprite = ui.VisibilitySprite;
            colorImage.color = ui.Color;
            foldoutImage.enabled = ui.hasChildren;
            layerTypeImage.sprite = ui.LayerTypeSprite;
            layerNameText.text = ui.LayerName;
        }
        
        void Update()
        {
            CalculateNewPosition();
        }

        private void CalculateNewPosition()
        {
            var pointerPosition = Pointer.current.position.ReadValue();
            var newPos = new Vector2(transform.position.x, pointerPosition.y + DragStartOffset.y);
            transform.position = newPos;
        }
    }
}