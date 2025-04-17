using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D
{
    public class ColorSwatch : MonoBehaviour, IPointerUpHandler
    {
        public Button Button => button;
        public bool IsSelected => isSelected;

        [SerializeField] private Image tileColor;
        [SerializeField] private Button button;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Image selection;
        [SerializeField] private Sprite selectionSprite;
        [SerializeField] private Sprite normalSprite;

        //public Color selectedColor = Color.green;
        //public Color normalColor = Color.white;

        public UnityEvent onClickUp = new();

        private bool isSelected = false;

        void Awake()
        {
            SetSelected(false);
        }

        public void SetSelected(bool isSelected)
        {
            this.isSelected = isSelected;
            UpdateVisual();
        }

        void UpdateVisual()
        {
            selection.sprite = isSelected ? selectionSprite : normalSprite;
            selection.type = Image.Type.Sliced;
            selection.pixelsPerUnitMultiplier = 32;
        }

        public void SetInputText(string text)
        {
            inputField.text = text;
        }

        public void SetColor(Color color)
        {
            tileColor.color = color;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            onClickUp.Invoke();
        }
    }
}
