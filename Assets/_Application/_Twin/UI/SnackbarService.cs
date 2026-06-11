using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.UI.Panels;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Services
{
    public class SnackbarService : MonoBehaviour
    {
        [SerializeField] private float waitTime = 5f;
        [SerializeField] private Color infoColor = Color.black;
        [SerializeField] private Color errorColor = Color.red;

        private SnackbarPanel snackbarPanel;
        private string activeMessage;
        private int activeCounter;
        private Coroutine activeCoroutine;
        private float timer;

        private void Start()
        {
            snackbarPanel = App.UIRoot.Root.Q<SnackbarPanel>();
            snackbarPanel.Show(false);           
        }

        private void OnEnable()
        {
            App.Layers.LayerAdded.AddListener(OnLayerAdded);
        }

        private void OnDisable()
        {
            App.Layers.LayerAdded.RemoveListener(OnLayerAdded);
        }

        private void OnLayerAdded(LayerData layerData)
        {
            if (activeCounter > 0)
                activeMessage += $" ,{layerData.Name}";
            else
                activeMessage += layerData.Name;
            activeCounter++;
            DisplayMessage(activeMessage + (activeCounter == 1 ? " is" : " zijn") + " succesvol toegevoegd");
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
            activeMessage = string.Empty;
            activeCounter = 0;
            snackbarPanel.Show(false);
        }
    }
}