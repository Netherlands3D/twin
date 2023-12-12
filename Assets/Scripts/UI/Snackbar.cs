using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class Snackbar : MonoBehaviour
    {
        [SerializeField] private float waitTime = 5f;
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI text;

        public static UnityEvent<string> SnackbarMessageEvent = new();
        private Coroutine activeCoroutine;

        private void Awake()
        {
            DisableSnackbar();
            SnackbarMessageEvent.AddListener(DisplayMessage);
        }

        private void OnDestroy()
        {
            SnackbarMessageEvent.RemoveListener(DisplayMessage);
        }

        private void OnEnable()
        {
            activeCoroutine = StartCoroutine(StartTimer());
        }

        private void OnDisable()
        {
            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            DisplayMessage(text.text);
        }
#endif

        public void DisplayMessage(string newText)
        {
            DisableSnackbar();
            text.text = newText;
            RecalculateHeight();    
            gameObject.SetActive(true);
        }

        private void RecalculateHeight()
        {
            var rectTransform = GetComponent<RectTransform>();
            var margin = text.GetComponent<RectTransform>().anchoredPosition.y;
            margin = Mathf.Abs(margin);
            rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, text.preferredHeight + 2 * margin);
        }

        private IEnumerator StartTimer()
        {
            slider.maxValue = waitTime;
            slider.value = slider.maxValue;

            while (slider.value > 0)
            {
                slider.value -= Time.deltaTime;
                yield return null;
            }

            DisableSnackbar();
        }

        //also called by button
        public void DisableSnackbar()
        {
            slider.value = 0;
            gameObject.SetActive(false);
            activeCoroutine = null;
        }
    }
}