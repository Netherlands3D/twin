using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class Snackbar : MonoBehaviour
    {
        [SerializeField] private float waitTime = 5f;
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI text;

        public static Snackbar Instance { get; private set; }
        private Coroutine activeCoroutine;

        private void Awake()
        {
            if (Instance)
                Destroy(Instance.gameObject);
            
            Instance = this;
            DisableSnackbar();
        }

        private void OnEnable()
        {
            activeCoroutine = StartCoroutine(StartTimer());
        }

        private void OnDisable()
        {
            if(activeCoroutine != null)
                StopCoroutine(activeCoroutine);
        }

        public void DisplayMessage(string newText)
        {
            DisableSnackbar();
            text.text = newText;
            gameObject.SetActive(true);
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