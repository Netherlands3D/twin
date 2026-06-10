using Netherlands3D.UI.Panels;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI
{
    public class SnackbarService : MonoBehaviour
    {
        [SerializeField] private float waitTime = 5f;
        [SerializeField] private Color infoColor = Color.black;
        [SerializeField] private Color errorColor = Color.red;

        private SnackbarPanel snackbarPanel;

        private Coroutine activeCoroutine;
        private float timer;

        private void Start()
        {
            snackbarPanel = App.UIRoot.Root.Q<SnackbarPanel>();
            snackbarPanel.Show(false);
        }

        public void DisplayMessage(string newText)
        {
            DisplayText(newText, infoColor);
        }

        public void DisplayError(string newText)
        {
            DisplayText(newText, errorColor);
        }

        private void DisplayText(string newText, Color color)
        {
            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);
            snackbarPanel.SetText(newText);
            snackbarPanel.SetTextColor(color);
            activeCoroutine = StartCoroutine(StartTimer());
        }

        private IEnumerator StartTimer()
        {
            //slider.maxValue = waitTime;
            //slider.value = slider.maxValue;
            snackbarPanel.Show(true);
            timer = waitTime;
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }
            snackbarPanel.Show(false);
        }
    }
}