using DG.Tweening;
using Netherlands3D.FirstPersonViewer.Events;
using Netherlands3D.Twin.Samplers;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerUIController : MonoBehaviour
    {
        [SerializeField] private GameObject viewerUI;
        private CanvasGroup viewerGroup;
        [SerializeField] private List<GameObject> uiToDisable;


        private PointerToWorldPosition pointerToWorld;




        private void Start()
        {
            pointerToWorld = FindFirstObjectByType<PointerToWorldPosition>();
            viewerGroup = viewerUI.GetComponent<CanvasGroup>();

            ViewerEvents.OnViewerEntered += EnterViewer;
            ViewerEvents.OnViewerExited += ExitViewer;
            ViewerEvents.OnHideUI += HideUI;

            viewerUI.SetActive(false);
        }

        private void OnDestroy()
        {
            ViewerEvents.OnViewerEntered -= EnterViewer;
            ViewerEvents.OnViewerExited -= ExitViewer;
            ViewerEvents.OnHideUI -= HideUI;
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

            pointerToWorld.SetActiveCamera(FirstPersonViewerData.Instance.FPVCamera);
        }

        private void ExitViewer()
        {
            viewerUI?.SetActive(false);
            uiToDisable.ForEach(ui => ui.SetActive(true));

            pointerToWorld.SetActiveCamera(Camera.main);
        }

        /// <summary>
        /// Temp Function for UI hiding (Will be replaced by the UI Hider)
        /// </summary>
        private void HideUI()
        {
            viewerUI.SetActive(!viewerUI.activeSelf);
        }
    }
}
