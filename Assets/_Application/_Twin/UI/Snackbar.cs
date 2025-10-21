using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI
{
    public class Snackbar : MonoBehaviour
    {
        [SerializeField] private float waitTime = 5f;
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Color infoColor = Color.black;
        [SerializeField] private Color errorColor = Color.red;

        private Coroutine activeCoroutine;

        private void Start()
        {
            DisableSnackbar();
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

        public void DisplayMessage(string newText)
        {
            DisableSnackbar();
            text.text = newText;
            text.color = infoColor;
            gameObject.SetActive(true);
            StartCoroutine(RebuildLayout());
        }

        private IEnumerator RebuildLayout()
        {
            // Wait a frame
            yield return null;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        }

        public void DisplayError(string newText)
        {
            DisplayMessage(newText);
            text.color = errorColor;
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