using DG.Tweening;
using Netherlands3D.Services;
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
            ServiceLocator.GetService<FirstPersonViewer>().Input.ExitDuration += UpdateTimer;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            ServiceLocator.GetService<FirstPersonViewer>().Input.ExitDuration -= UpdateTimer;
        }

        public void UpdateTimer(float percentage)
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
