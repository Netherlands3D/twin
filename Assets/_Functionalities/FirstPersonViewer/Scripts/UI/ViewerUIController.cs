using DG.Tweening;
using Netherlands3D.Twin.Samplers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerUIController : MonoBehaviour
    {
        [SerializeField] private GameObject viewerUI;
        private CanvasGroup viewerGroup;
        [SerializeField] private List<GameObject> uiToDisable;

        private PointerToWorldPosition pointerToWorld;

        [Space(5)]
        [SerializeField] private InputActionReference hideButton;

        private void Start()
        {
            pointerToWorld = FindFirstObjectByType<PointerToWorldPosition>();
            viewerGroup = viewerUI.GetComponent<CanvasGroup>();

            FirstPersonViewer.OnViewerEntered += EnterViewer;
            FirstPersonViewer.OnViewerExited += ExitViewer;
            hideButton.action.performed += OnHideUIPressed;

            viewerUI.SetActive(false);
        }

        private void OnDestroy()
        {
            FirstPersonViewer.OnViewerEntered -= EnterViewer;
            FirstPersonViewer.OnViewerExited -= ExitViewer;
            hideButton.action.performed -= OnHideUIPressed;
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

            pointerToWorld.SetActiveCamera(FirstPersonViewerCamera.FPVCamera);
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
        public void HideUI()
        {
            viewerUI.SetActive(!viewerUI.activeSelf);
        }

        private void OnHideUIPressed(InputAction.CallbackContext context) => HideUI();
    }
}
