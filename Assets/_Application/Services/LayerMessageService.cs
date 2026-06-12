using Netherlands3D.Services;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Services;
using UnityEngine;

namespace Netherlands3D
{
    public class LayerMessageService : MonoBehaviour
    {
        private Layers layers;
        private SnackbarService snackbarService;

        private void Awake()
        {
            layers = App.Layers;
            snackbarService = App.Snackbar;
        }

        private void OnEnable()
        {
            layers.LayerAdded.AddListener(OnLayerAdded);
            layers.DataTypeChain.CouldNotFindAdapter.AddListener(CouldNotFindAdapterMessage);
            layers.DataTypeChain.OnDownloadFailed.AddListener(DownloadFailedMessage);
            layers.DataTypeChain.OnLocalCacheFailed.AddListener(LocalCacheFailedMessage);
        }

        private void OnDisable()
        {
            layers.LayerAdded.RemoveListener(OnLayerAdded);
            layers.DataTypeChain.CouldNotFindAdapter.RemoveListener(CouldNotFindAdapterMessage);
            layers.DataTypeChain.OnDownloadFailed.RemoveListener(DownloadFailedMessage);
            layers.DataTypeChain.OnLocalCacheFailed.RemoveListener(LocalCacheFailedMessage);
        }

        private void OnLayerAdded(LayerData layerData)
        {
            snackbarService.OnLayerAdded(layerData);
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
    }
}
