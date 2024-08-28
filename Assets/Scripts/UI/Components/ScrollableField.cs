using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class ScrollableField : MonoBehaviour, IScrollHandler
    {
        [SerializeField] private float sensitivity = 10f;
        [SerializeField] private int minValue = 0;
        [SerializeField] private int maxValue = 12;
        public UnityEvent<int> fieldChanged;

        private InputField field;
        private TMP_InputField tmp_field;

        private void Start()
        {
            field = GetComponent<InputField>();
            tmp_field = GetComponent<TMP_InputField>();
        }

        public void OnScroll(PointerEventData eventData)
        {
            var sign = eventData.scrollDelta.x + eventData.scrollDelta.y < 0 ? -1 : 1;
            var delta = eventData.scrollDelta.magnitude * sensitivity * sign;

            if (tmp_field)
            {
                tmp_field.DeactivateInputField(); //deactivate the input field because it will then update to the new value 
                IncrementFieldValue(tmp_field.text, delta);
            }

            if (field)
            {
                field.DeactivateInputField(); //deactivate the input field because it will then update to the new value
                IncrementFieldValue(field.text, delta);
            }
        }

        private void IncrementFieldValue(string currentValue, float delta)
        {
            if (!int.TryParse(currentValue, out int parsedInt))
            {
                return;
            }

            parsedInt += (int)delta;
            parsedInt = Math.Clamp(parsedInt, minValue, maxValue);

            fieldChanged.Invoke(parsedInt);
        }
    }
}