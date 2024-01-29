using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI
{
    [RequireComponent(typeof(Outline))]
    [RequireComponent(typeof(Image))]
    public class TabButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
    {
        public TabGroup tabGroup;
        public GameObject tabPane;

        private TextMeshProUGUI text;
        private Outline outline;
        private Image image;

        private void Start()
        {
            text = GetComponentInChildren<TextMeshProUGUI>();
            outline = GetComponent<Outline>();
            image = GetComponent<Image>();

            // tabGroup.Subscribe(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            tabGroup.OnTabEnter(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            tabGroup.OnTabSelected(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tabGroup.OnTabExit(this);
        }

        public void SetOutline(int width, Color color)
        {
            outline.effectDistance = Vector2.one * -width;
            outline.effectColor = color;
        }

        public void SetColors(Color backgroundColor, Color textColor)
        {
            image.color = backgroundColor;
            text.color = textColor;
        }
    }
}
