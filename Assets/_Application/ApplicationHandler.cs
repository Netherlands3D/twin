using Netherlands3D.Services;
using Netherlands3D.Twin.Services;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin
{
    public class ApplicationHandler : MonoBehaviour
    {
        private void OnEnable()
        {
            InputService input =  ServiceLocator.GetService<InputService>();
            input.ExitAction.performed += OnExitApplication;
        }
        
        private void OnDisable()
        {
            InputService input =  ServiceLocator.GetService<InputService>();
            input.ExitAction.performed -= OnExitApplication;
        }
        
        private void OnExitApplication(InputAction.CallbackContext ctx) => PromptExitDialog();

        public void PromptExitDialog()
        {
#if !UNITY_EDITOR && UNITY_WEBGL
            return;
#endif
            Dialog yesNoDialog = ServiceLocator.GetService<DialogService>().ActiveDialog;
            yesNoDialog.Show(true);
            yesNoDialog.Accept.AddListener(ApplicationQuit);
        }

        private void ApplicationQuit()
        {
            Application.Quit();
        }
    }
}
