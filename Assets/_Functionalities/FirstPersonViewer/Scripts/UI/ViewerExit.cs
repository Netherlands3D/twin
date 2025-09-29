using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerExit : MonoBehaviour
    {
        [SerializeField] private Slider timerSlider;

        private void Awake()
        {
            ViewerEvents.ExitDuration += UpdateTimer;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            ViewerEvents.ExitDuration -= UpdateTimer;
        }

        private void UpdateTimer(float percentage)
        {
            if (percentage == -1) gameObject.SetActive(false);
            else
            {
                gameObject.SetActive(true);
                timerSlider.value = percentage;
            }
        }
    }
}
