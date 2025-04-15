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
