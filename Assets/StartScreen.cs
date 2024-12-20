using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class StartScreen : MonoBehaviour
    {
        public UnityEvent getReady = new();
        public UnityEvent startGame = new ();
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private CanvasGroup CountdownOne;
        [SerializeField] private CanvasGroup CountdownTwo;
        [SerializeField] private CanvasGroup CountdownThree;
        [SerializeField] private float fadeDuration = .3f;
        [SerializeField] private float countDownDuration = 1f;
        [SerializeField] private float countDownTargetScale = .5f;

        private void Start()
        {
            Show();
        }

        private void OnEnable()
        {
            CountdownThree.alpha = 1f;
            CountdownThree.gameObject.SetActive(false);
            CountdownTwo.alpha = 1f;
            CountdownTwo.gameObject.SetActive(false);
            CountdownOne.alpha = 1f;
            CountdownOne.gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void StartGame()
        {
            var sequence = DOTween.Sequence(this);
            sequence.Append(canvasGroup.DOFade(0, fadeDuration).OnComplete(TriggerGetReady));
            sequence.AppendCallback(() => CountdownThree.gameObject.SetActive(true));
            sequence.Append(CountdownThree.transform.DOScale(countDownTargetScale, countDownDuration));
            sequence.Join(CountdownThree.DOFade(0, countDownDuration).SetEase(Ease.InQuad));
            sequence.AppendCallback(() => CountdownThree.gameObject.SetActive(false));
            sequence.AppendCallback(() => CountdownTwo.gameObject.SetActive(true));
            sequence.Append(CountdownTwo.transform.DOScale(countDownTargetScale, countDownDuration));
            sequence.Join(CountdownTwo.DOFade(0, countDownDuration).SetEase(Ease.InQuad));
            sequence.AppendCallback(() => CountdownTwo.gameObject.SetActive(false));
            sequence.AppendCallback(() => CountdownOne.gameObject.SetActive(true));
            sequence.Append(CountdownOne.transform.DOScale(countDownTargetScale, countDownDuration));
            sequence.Join(CountdownOne.DOFade(0, countDownDuration).SetEase(Ease.InQuad));
            sequence.AppendCallback(() => CountdownOne.gameObject.SetActive(false));
            sequence.AppendCallback(TriggerStartOfGame);
            sequence.Play();
        }

        private void TriggerGetReady()
        {
            gameObject.SetActive(false);
            canvasGroup.alpha = 1f;
            getReady.Invoke();
        }

        private void TriggerStartOfGame()
        {
            startGame.Invoke();
        }
    }
}
