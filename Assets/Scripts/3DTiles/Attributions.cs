using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Netherlands3D.Twin
{
    public class Attributions : MonoBehaviour
    {
        [SerializeField] private Image attributionsIconImage;
        [SerializeField] private TMP_Text attributionsText;

        public Image AttributionsIconImage { get => attributionsIconImage; set => attributionsIconImage = value; }
        public TMP_Text AttributionsText { get => attributionsText; set => attributionsText = value; }

        private void Awake() {
            AttributionsIconImage.gameObject.SetActive(false);
            attributionsText.text = "";
        }

        public void SetAttributionsText(string attributions)
        {
            AttributionsText.text = attributions;
            var hasText = !string.IsNullOrEmpty(attributions);
            
            AttributionsIconImage.gameObject.SetActive(hasText);
        }
    }
}
