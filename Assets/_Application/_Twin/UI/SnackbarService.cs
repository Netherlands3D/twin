using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using Netherlands3D.UI_Toolkit.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Services
{
    public class SnackbarService : MonoBehaviour
    {
        private const float defaultWaitTime = 8f;

        private SnackbarPanel snackbarPanel;       
        private readonly Dictionary<float, WaitForSeconds> waitForSecondsCache = new();

        [SerializeField] private string defaultInfoIcon = IconImage.CHECKMARK;
        [SerializeField] private string defaultWarningIcon = IconImage.WARNING;

        public UnityEvent OnShowMessage = new();
        public UnityEvent OnHideMessage = new();

        private void Start()
        {
            snackbarPanel = App.UIRoot.Root.Q<SnackbarPanel>();
            snackbarPanel.Show(false);           
        }

        public void DisplayMessage(string newText, float time = defaultWaitTime)
        {
            DisplayText(newText, string.Empty, SnackBarItem.SnackbarMessageType.Info, defaultInfoIcon, time);
        }

        public void DisplayError(string newText, float time = defaultWaitTime)
        {
            DisplayText(newText, string.Empty, SnackBarItem.SnackbarMessageType.Warning, defaultWarningIcon, time);
        }

        public void DisplayMessage(string newText, string icon, float time = defaultWaitTime)
        {
            DisplayText(newText, string.Empty, SnackBarItem.SnackbarMessageType.Info, icon, time);
        }

        private SnackBarItem DisplayText(string title, string details, SnackBarItem.SnackbarMessageType type, string icon, float time = defaultWaitTime)
        {
            var item = snackbarPanel.SetMessage(title, details, type, icon);
            StartCoroutine(StartTimer(item, time));

            return item;
        }

        private WaitForSeconds GetWaitForSeconds(float duration)
        {
            if (!waitForSecondsCache.TryGetValue(duration, out var waitForSeconds))
            {
                waitForSeconds = new WaitForSeconds(duration);
                waitForSecondsCache[duration] = waitForSeconds;
            }

            return waitForSeconds;
        }

        private IEnumerator StartTimer(SnackBarItem item, float duration)
        {
            //TODO UI Toolkit, implement a slider here in the panel so the timer is visible to the user.
            OnShowMessage.Invoke();

            yield return GetWaitForSeconds(duration);
            if (item.IsClosed) yield break;
            
            snackbarPanel.RemoveItem(item);
            OnHideMessage.Invoke();
        }
    }
}