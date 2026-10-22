using Netherlands3D.Twin;
using Netherlands3D.Twin.Services;
using UnityEngine;

namespace Netherlands3D.Services
{
    public class DebugMessageService : MonoBehaviour
    {
        private SnackbarService snackbarService;

        private void Awake()
        {
            snackbarService = App.Snackbar;
        }

        public void DisplayMessage(string message)
        {
            snackbarService.DisplayMessage(message);
        }

        public void DisplayMessage(string message, string icon)
        {
            snackbarService.DisplayMessage(message, icon);
        }

        public void DisplayWarning(string message)
        {
            snackbarService.DisplayError(message);
        }
    }
}