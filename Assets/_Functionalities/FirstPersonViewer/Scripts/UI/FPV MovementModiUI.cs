using Netherlands3D.FirstPersonViewer;
using Netherlands3D.FirstPersonViewer.ViewModus;
using Netherlands3D.Services;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class FPVMovementModiUI : MonoBehaviour
    {
        [SerializeField] private Image currentMovemodeImage;

        [SerializeField] private float fadeInLength;
        [SerializeField] private float intervalLength;
        [SerializeField] private float fadeOutLength;

        private MovementModusSwitcher switcher;
        private Sequence uiSequence;

        private void OnEnable()
        {
            FirstPersonViewer fpv = ServiceLocator.GetService<FirstPersonViewer>();

            switcher = fpv.MovementSwitcher;
            switcher.OnMovementPresetChanged += SwitchModeUI;
        }

        private void OnDisable()
        {
            if (switcher != null) switcher.OnMovementPresetChanged -= SwitchModeUI;
        }

        private void SwitchModeUI(ViewerState state)
        {
            currentMovemodeImage.sprite = state.viewIcon;

            //Kill previous UI Sequence and start a new one.
            uiSequence?.Kill();
            uiSequence = DOTween.Sequence();
            uiSequence.AppendCallback(() => currentMovemodeImage.color = new Color(1,1,1,0));
            uiSequence.Append(currentMovemodeImage.DOFade(0.48f, fadeInLength).SetEase(Ease.OutSine));
            uiSequence.AppendInterval(intervalLength);
            uiSequence.Append(currentMovemodeImage.DOFade(0, fadeOutLength).SetEase(Ease.InSine));
        }


    }
}
