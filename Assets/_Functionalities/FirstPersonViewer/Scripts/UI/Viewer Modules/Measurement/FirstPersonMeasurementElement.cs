using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurementElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI firstPointText;
        [SerializeField] private TextMeshProUGUI secondPointText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private Image arrowImage;

        [SerializeField] private CanvasGroup deleteButtonGroup;

        private Action<FirstPersonMeasurementElement> onDeleteCallback;
        
        
        
        public void Init(string firstLetter, string secondLetter, float dstInMeters, Color color, Action<FirstPersonMeasurementElement> onDeleteCallback)
        {
            firstPointText.text = firstLetter;
            secondPointText.text = secondLetter;

            distanceText.text = FirstPersonMeasurement.ConvertToUnits(dstInMeters);
            distanceText.color = color;
            arrowImage.color = color;

            this.onDeleteCallback = onDeleteCallback;
        }

        public void UpdateMeasurement(string firstLetter, string secondLetter, float dstInMeters)
        {
            firstPointText.text = firstLetter;
            secondPointText.text = secondLetter;
            distanceText.text = FirstPersonMeasurement.ConvertToUnits(dstInMeters);
        }

        public void SetTextColor(Color color)
        {
            distanceText.color = color;
            arrowImage.color = color;
        }

        public void RemoveMeasurement() => onDeleteCallback?.Invoke(this);

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
