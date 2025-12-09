using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D
{
    public class FirstPersonMeasurementElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI firstPointText;
        [SerializeField] private TextMeshProUGUI secondPointText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private Image arrowImage;

        [SerializeField] private CanvasGroup deleteButtonGroup;

        private Action<FirstPersonMeasurementElement> onDeleteCallback;

        public void Init(string firstLetter, string secondLetter, float dstInMeters, Color32 color, Action<FirstPersonMeasurementElement> onDeleteCallback)
        {
            firstPointText.text = firstLetter;
            secondPointText.text = secondLetter;

            distanceText.text = ConvertToUnits(dstInMeters);
            distanceText.color = color;
            arrowImage.color = color;

            this.onDeleteCallback = onDeleteCallback;
        }

        public void UpdateMeasurement(string firstLetter, string secondLetter, float dstInMeters)
        {
            firstPointText.text = firstLetter;
            secondPointText.text = secondLetter;
            distanceText.text = ConvertToUnits(dstInMeters);
        }

        private string ConvertToUnits(float valueInMeters)
        {
            string units = "m";

            if(valueInMeters >= 1000)
            {
                units = "km";
                valueInMeters /= 1000;
            }

            float roundedValue = Mathf.Round(valueInMeters * 100) / 100;
            return "~" + roundedValue + units;
        }
        
        public void RemoveMeasurement()
        {
            onDeleteCallback?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            deleteButtonGroup.alpha = 0;
            deleteButtonGroup.gameObject.SetActive(true);

            deleteButtonGroup.DOKill();
            deleteButtonGroup.DOFade(1, .3f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            deleteButtonGroup.DOKill();
            deleteButtonGroup.DOFade(0, .3f).OnComplete(() => deleteButtonGroup.gameObject.SetActive(false));
        }
    }
}
