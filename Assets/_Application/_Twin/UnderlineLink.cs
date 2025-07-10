using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Netherlands3D
{
    public class UnderlineLink : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TextMeshProUGUI tmp;

        private void Awake()
        {
            tmp = GetComponent<TextMeshProUGUI>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetUnderline(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetUnderline(false);
        }

        private void SetUnderline(bool underline)
        {
            if (underline)
                tmp.fontStyle = FontStyles.Underline;
            else
                tmp.fontStyle = FontStyles.Normal;
        }
    }
}
