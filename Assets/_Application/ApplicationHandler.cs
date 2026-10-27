using Netherlands3D.Services;
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
            Debug.Log("Prompt Exit Dialog");
        }
    }
}
