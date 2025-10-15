using DG.Tweening;
using Netherlands3D.FirstPersonViewer.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerExit : MonoBehaviour
    {
        [SerializeField] private CanvasGroup exitGroup;
        [SerializeField] private Slider timerSlider;

        private void Start()
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
            if (percentage == -1)
            {
                exitGroup.DOKill();
                exitGroup.alpha = 0;
                gameObject.SetActive(false);
            }
            else
            {
                if (percentage <= 1) exitGroup.DOFade(1, .5f);

                gameObject.SetActive(true);
                timerSlider.value = percentage;
            }
        }
    }
}
