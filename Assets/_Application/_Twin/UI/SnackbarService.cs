using Netherlands3D.Twin;
using Netherlands3D.UI.Panels;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.Services
{
    public class SnackbarService : MonoBehaviour
    {
        private const float defaultWaitTime = 5f;
        [SerializeField] private Color infoColor = Color.black;
        [SerializeField] private Color errorColor = Color.red;

        private SnackbarPanel snackbarPanel;       
        private Coroutine activeCoroutine;
        private float timer;

        public UnityEvent OnShowMessage = new();
        public UnityEvent OnHideMessage = new();

        private void Start()
        {
            snackbarPanel = App.UIRoot.Root.Q<SnackbarPanel>();
            snackbarPanel.Show(false);           
        }

        public void DisplayMessage(string newText, float time = defaultWaitTime)
        {
            DisplayText(newText, infoColor, time);
        }

        public void DisplayError(string newText, float time = defaultWaitTime)
        {
            DisplayText(newText, errorColor, time);
        }

        public void DisplayMessage(string newText, Color color, float time = defaultWaitTime)
        {
            DisplayText(newText, color, time);
        }       

        private void DisplayText(string newText, Color color, float time = defaultWaitTime)
        {
            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);
            snackbarPanel.SetText(newText);
            snackbarPanel.SetTextColor(color);
            activeCoroutine = StartCoroutine(StartTimer(time));
        }

        private IEnumerator StartTimer(float duration)
        {
            //TODO UI Toolkit, implement a slider here in the panel so the timer is visible to the user.
            snackbarPanel.Show(true);
            OnShowMessage.Invoke();
            timer = duration;
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }         
            snackbarPanel.Show(false);
            OnHideMessage.Invoke();
        }
    }
}