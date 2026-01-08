using DG.Tweening;
using Netherlands3D.Services;
using Netherlands3D.Events;
using Netherlands3D.Twin.Samplers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Netherlands3D.FirstPersonViewer.ViewModus;

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

        private bool isInFPV;

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

        private void EnterViewer(ViewerState state, Dictionary<string, object> settings)
        {
            if (isInFPV) return;
            isInFPV = true;

            uiToDisable.ForEach(ui => ui.SetActive(false));

            if (viewerGroup != null)
            {
                viewerGroup.alpha = 0;
                viewerUI.SetActive(false);
                viewerGroup.DOFade(1, 1f).SetDelay(1);
            }
            viewerUI.SetActive(true);

            pointerToWorld.SetActiveCamera(FirstPersonViewerCamera.FPVCamera);
        }

        private void ExitViewer(bool modified)
        {
            viewerUI?.SetActive(false);
            uiToDisable.ForEach(ui => ui.SetActive(true));

            pointerToWorld.SetActiveCamera(Camera.main);
            isInFPV = false;
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
