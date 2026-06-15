using Netherlands3D.Twin.Layers;
using UnityEngine;

namespace Netherlands3D.Twin.Services
{
    public class LayerMessageService : MonoBehaviour
    {
        private Layers layers;
        private SnackbarService snackbarService;
        private string activeMessage;
        private int activeCounter;

        private void Awake()
        {
            layers = App.Layers;
            snackbarService = App.Snackbar;
        }

        private void OnEnable()
        {
            snackbarService.OnHideMessage.AddListener(OnHideSnackbar);
            layers.LayerAdded.AddListener(OnLayerAdded);
            layers.VisualizationCreated.AddListener(OnVisualizationCreated); // when the visualisation is created, we want to listen to potential error messages (eg. parse errors) to display
            layers.DataTypeChain.CouldNotFindAdapter.AddListener(CouldNotFindAdapterMessage);
            layers.DataTypeChain.OnDownloadFailed.AddListener(DownloadFailedMessage);
            layers.DataTypeChain.OnLocalCacheFailed.AddListener(LocalCacheFailedMessage);
        }

        private void OnDisable()
        {
            snackbarService.OnHideMessage.RemoveListener(OnHideSnackbar);
            layers.LayerAdded.RemoveListener(OnLayerAdded);
            layers.VisualizationCreated.RemoveListener(OnVisualizationCreated);
            layers.DataTypeChain.CouldNotFindAdapter.RemoveListener(CouldNotFindAdapterMessage);
            layers.DataTypeChain.OnDownloadFailed.RemoveListener(DownloadFailedMessage);
            layers.DataTypeChain.OnLocalCacheFailed.RemoveListener(LocalCacheFailedMessage);
        }

        private void OnVisualizationCreated(LayerGameObject visualization)
        {
            visualization.VisualisationError.AddListener(VisualizationErrorMessage);
        }

        private void OnHideSnackbar()
        {              
            activeMessage = string.Empty;
            activeCounter = 0;
        }

        private void OnLayerAdded(LayerData layerData)
        {
            if (activeCounter > 0)
                activeMessage += $" ,{layerData.Name}";
            else
                activeMessage += layerData.Name;
            activeCounter++;
            snackbarService.DisplayMessage(activeMessage + (activeCounter == 1 ? " is" : " zijn") + " succesvol toegevoegd");
        }

        private void CouldNotFindAdapterMessage(string message)
        {
            snackbarService.DisplayError($"Dit type brondata wordt niet ondersteund voor: {message}, probeer een andere.");
        }

        private void DownloadFailedMessage(string message)
        {
            snackbarService.DisplayError($"Er is iets mis gegaan bij het downloaden van deze bron: {message}, probeer het opnieuw of controleer de CORS instellingen.");
        }

        private void LocalCacheFailedMessage(string message)
        {
            snackbarService.DisplayError($"Er is een leeg bestand ontvangen tijdens het downloaden van deze bron: {message}, probeer het opnieuw of controleer de CORS instellingen.");
        }
        
        private void VisualizationErrorMessage(string message)
        {
            snackbarService.DisplayError(message);
        }
    }
}
