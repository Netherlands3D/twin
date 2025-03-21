using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.Sun
{
    public class ScrollableTimeField : MonoBehaviour, IScrollHandler
    {
        [SerializeField] private float sensitivity = 10f;
        public UnityEvent<int> minutesChanged;
        public UnityEvent<int> hoursChanged;

        private InputField field;
        private TMP_InputField tmp_field;
        private TimeParser parser;

        private void Start()
        {
            field = GetComponent<InputField>();
            tmp_field = GetComponent<TMP_InputField>();
            parser = GetComponent<TimeParser>();
        }

        public void OnScroll(PointerEventData eventData)
        {
            var sign = eventData.scrollDelta.x + eventData.scrollDelta.y < 0 ? -1 : 1;
            var delta = eventData.scrollDelta.magnitude * sensitivity * sign;

            if (tmp_field)
            {
                var isFocused = tmp_field.isFocused;
                tmp_field.DeactivateInputField(); //deactivate the input field because it will then update to the new value
                IncrementFieldValue(tmp_field.text, delta);
                if (isFocused)
                    tmp_field.ActivateInputField();
            }

            if (field)
            {
                var isFocused = field.isFocused;
                field.DeactivateInputField(); //deactivate the input field because it will then update to the new value
                IncrementFieldValue(field.text, delta);
                if (isFocused)
                    field.ActivateInputField();
            }
        }

        private void IncrementFieldValue(string currentValue, float delta)
        {
            var dateTime = parser.ParseTime(currentValue);
            var newTime = dateTime.AddMinutes(delta);
            minutesChanged.Invoke(newTime.Minute);
            hoursChanged.Invoke(newTime.Hour);
        }
    }
}
