using System;
using Netherlands3D.DataTypeAdapters;
using Netherlands3D.Events;
using Netherlands3D.Twin.Layers;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Services
{
    public class LayerMessageService : MonoBehaviour
    {
        private Layers layers;
        private SnackbarService snackbarService;
        private string activeAddedMessage;
        private string activeRemovalMessage;
        private int activeAddedCounter;
        private int activeRemovalCounter;
        private DataTypeChain[] chains;
        
        [SerializeField] private StringEvent layerSourceAttributionEvent;
        public UnityEvent<string> OnAttributionReceived;

        private bool messageAddedDirty = false;
        private bool messageRemovalDirty = false;
        
        private void Awake()
        {
            layers = App.Layers;
            snackbarService = App.Snackbar;
            chains = FindObjectsByType<DataTypeChain>(FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            snackbarService.OnHideMessage.AddListener(OnHideSnackbar);
            layers.LayerAdded.AddListener(OnLayerAdded);
            layers.LayerRemoved.AddListener(OnLayerRemoved);
            layers.VisualizationCreated.AddListener(OnVisualizationCreated); // when the visualisation is created, we want to listen to potential error messages (eg. parse errors) to display
            layerSourceAttributionEvent.AddListenerStarted(OnAttributionReceived.Invoke);

            foreach (var chain in chains)
            {
                chain.CouldNotFindAdapter.AddListener(CouldNotFindAdapterMessage);
                chain.OnDownloadFailed.AddListener(DownloadFailedMessage);
                chain.OnLocalCacheFailed.AddListener(LocalCacheFailedMessage);
            }
        }

        private void OnDisable()
        {
            snackbarService.OnHideMessage.RemoveListener(OnHideSnackbar);
            layers.LayerAdded.RemoveListener(OnLayerAdded);
            layers.LayerRemoved.RemoveListener(OnLayerRemoved);
            layers.VisualizationCreated.RemoveListener(OnVisualizationCreated);
            layerSourceAttributionEvent.RemoveListenerStarted(OnAttributionReceived.Invoke);
            
            foreach (var chain in chains)
            {
                chain.CouldNotFindAdapter.RemoveListener(CouldNotFindAdapterMessage);
                chain.OnDownloadFailed.RemoveListener(DownloadFailedMessage);
                chain.OnLocalCacheFailed.RemoveListener(LocalCacheFailedMessage);
            }
        }

        private void OnVisualizationCreated(LayerGameObject visualization)
        {
            //visualisationerror is automatically cleared when the visualisation is destroyed
            visualization?.VisualisationError.AddListener(VisualizationErrorMessage);
        }

        private void OnHideSnackbar()
        {              
            activeAddedMessage = string.Empty;
            activeAddedCounter = 0;
        }

        private void OnLayerAdded(LayerData layerData)
        {
            if (activeAddedCounter > 0)
                activeAddedMessage += $" ,{layerData.Name}";
            else
                activeAddedMessage += layerData.Name;
            activeAddedCounter++;
            messageAddedDirty = true;
        }

        //todo switch counter when adding -> removing or removing -> adding
        private void OnLayerRemoved(LayerData layerData)
        {
            if (activeRemovalCounter > 0)
                activeRemovalMessage += $" ,{layerData.Name}";
            else
                activeRemovalMessage += layerData.Name;
            activeRemovalCounter++;
            messageRemovalDirty = true;
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

        // TODO: Replace this specific method with a generic layer message flow.
        // Now only used in Tile3DLayerGameObject.cs
        public void UnsupportedExtensionsMessage(string message)
        {
            snackbarService.DisplayError(message);
        }

        // TODO: Replace this specific CSV message with a generic layer replacement message flow.
        public void DisplayCsvReplacedMessage(string message)
        {
            snackbarService.DisplayMessage(message, IconImage.SHEETS);
        }

        private void LateUpdate()
        {
            if (messageAddedDirty)
            {
                messageAddedDirty = false;
                snackbarService.DisplayMessage(activeAddedMessage + (activeAddedCounter == 1 ? " is" : " zijn") + " succesvol toegevoegd", IconImage.SHEETS);
                activeAddedMessage = string.Empty;
                activeAddedCounter = 0;
            }
            if (messageRemovalDirty)
            {
                messageRemovalDirty = false;
                snackbarService.DisplayMessage(activeRemovalMessage + (activeRemovalCounter == 1 ? " is" : " zijn") + " succesvol verwijderd");
                activeRemovalMessage = string.Empty;
                activeRemovalCounter = 0;
            }
        }
    }
}
