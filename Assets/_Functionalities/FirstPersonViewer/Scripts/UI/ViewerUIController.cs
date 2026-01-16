using DG.Tweening;
using Netherlands3D.Services;
using Netherlands3D.Events;
using Netherlands3D.Twin.Samplers;
using System.Collections.Generic;
using Netherlands3D.Twin.Cameras;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.UI
{
    public class ViewerUIController : MonoBehaviour
    {
        private CanvasGroup viewerGroup;
        [SerializeField] private GameObject viewerUI;
        [SerializeField] private List<GameObject> uiToDisable;

        [Space(5)]
        [SerializeField] private InputActionReference hideButton;

        [Header("Snackbar")]
        [SerializeField] private StringEvent snackbarEvent;
        [SerializeField] private string uiHideText;

        private PointerToWorldPosition pointerToWorld;
        private FirstPersonViewer firstPersonViewer;

        private void Start()
        {
            pointerToWorld = FindFirstObjectByType<PointerToWorldPosition>();
            viewerGroup = viewerUI.GetComponent<CanvasGroup>();

            //Events get cleared in First Person Viewer code.
            firstPersonViewer = ServiceLocator.GetService<FirstPersonViewer>();
            firstPersonViewer.OnViewerEntered += EnterViewer;
            firstPersonViewer.OnViewerExited += ExitViewer;

            hideButton.action.performed += OnHideUIPressed;

            viewerUI.SetActive(false);
        }

        private void OnDestroy()
        {
            hideButton.action.performed -= OnHideUIPressed;
        }

        private void EnterViewer()
        {
            uiToDisable.ForEach(ui => ui.SetActive(false));

            if (viewerGroup != null)
            {
                viewerGroup.alpha = 0;
                viewerUI.SetActive(false);
                viewerGroup.DOFade(1, 1f).SetDelay(1);
            }
            viewerUI.SetActive(true);

            Camera camera = ServiceLocator.GetService<FirstPersonViewer>().FirstPersonCamera.FPVCamera;
            pointerToWorld.SetActiveCamera(camera);
        }

        private void ExitViewer(bool modified)
        {
            viewerUI?.SetActive(false);
            uiToDisable.ForEach(ui => ui.SetActive(true));

            Camera camera = ServiceLocator.GetService<CameraService>().PreviousCamera;
            pointerToWorld.SetActiveCamera(camera);
        }

        /// <summary>
        /// Temp Function for UI hiding (Will be replaced by the UI Hider)
        /// </summary>
        public void HideUI()
        {
            viewerUI.SetActive(!viewerUI.activeSelf);
            if (!viewerUI.activeSelf) snackbarEvent.InvokeStarted(uiHideText);
        }

        private void OnHideUIPressed(InputAction.CallbackContext context) => HideUI();
    }
}
