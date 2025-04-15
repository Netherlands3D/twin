using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D
{
    public class ColorSwatch : MonoBehaviour
    {
        public Button Button => button;

        [SerializeField] private Image tileColor;
        [SerializeField] private Button button;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Image selection;

        public Color selectedColor = Color.green;
        public Color normalColor = Color.white;

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
            selection.color = isSelected ? selectedColor : normalColor;
        }

        public void SetInputText(string text)
        {
            inputField.text = text;
        }

        public void SetColor(Color color)
        {
            tileColor.color = color;
        }
    }
}
