using Netherlands3D.Twin;
using Netherlands3D.Twin.Services;
using Netherlands3D.UI_Toolkit.Scripts;
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

        public void DisplayWarning(string message)
        {
            snackbarService.DisplayError(message);
        }

        public void DisplayFpvUnlockMessage(string message)
        {
            snackbarService.DisplayMessage(message, IconImage.FPV);
        }

        public void DisplayFpvCopyCoordinatesMessage(string message)
        {
            snackbarService.DisplayMessage(message, IconImage.COPY_PASTE);
        }

        public void DisplayPresentationModeExitMessage()
        {
            snackbarService.DisplayMessage(
                "Druk op H om de presentatiemodus te verlaten.",
                IconImage.PRESENTATION_CHART
            );
        }

        public void DisplayFunctionPreferencesSavedMessage()
        {
            App.Snackbar.DisplayMessage(
                "Functie voorkeuren succesvol aangepast",
                IconImage.CHECKMARK
            );
        }
    }
}