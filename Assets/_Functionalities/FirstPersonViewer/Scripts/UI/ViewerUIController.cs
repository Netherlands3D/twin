using DG.Tweening;
using Netherlands3D.FirstPersonViewer.Events;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerUIController : MonoBehaviour
    {
        [SerializeField] private GameObject viewerUI;
        private CanvasGroup viewerGroup;
        [SerializeField] private List<GameObject> uiToDisable;

        private void Start()
        {
            viewerGroup = viewerUI.GetComponent<CanvasGroup>();

            ViewerEvents.OnViewerEntered += EnterViewer;
            ViewerEvents.OnViewerExited += ExitViewer;

            viewerUI.SetActive(false);
        }

        private void OnDestroy()
        {
            ViewerEvents.OnViewerEntered -= EnterViewer;
            ViewerEvents.OnViewerExited -= ExitViewer;
        }

        private void EnterViewer()
        {
            uiToDisable.ForEach(ui => ui.SetActive(false));

            if(viewerGroup != null)
            {
                viewerGroup.alpha = 0;
                viewerUI.SetActive(false);
                viewerGroup.DOFade(1, 1f).SetDelay(1);
            } viewerUI.SetActive(true);
        }

        private void ExitViewer()
        {
            viewerUI?.SetActive(false);
            uiToDisable.ForEach(ui => ui.SetActive(true));
        }
    }
}
