using System;
using GeoJSON.Net.Feature;
using Netherlands3D.Indicators.Dossiers;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Indicators
{
    public class DossierListener : MonoBehaviour
    {
        [SerializeField] private DossierSO Dossier;

        [SerializeField] private bool invokeOnStart = false;
        public UnityEvent onOpening = new();
        public UnityEvent<Dossier> onOpen = new();
        public UnityEvent<Variant?> onSelectedVariant = new();
        public UnityEvent onFailedToOpen = new();
        public UnityEvent onClose = new();
        public UnityEvent<FeatureCollection> onLoadedProjectArea = new();

        private void Start()
        {
            if (!invokeOnStart) return;

            switch (Dossier.State)
            {
                case DossierSO.DossierSystemState.Opening: onOpening.Invoke(); break;
                case DossierSO.DossierSystemState.Opened: if (Dossier.Data.HasValue) onOpen.Invoke(Dossier.Data.Value); break;
                case DossierSO.DossierSystemState.FailedToOpen: onFailedToOpen.Invoke(); break;
                case DossierSO.DossierSystemState.Closed: onClose.Invoke(); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private void OnEnable()
        {
            Dossier.onOpening.AddListener(OnOpening);
            Dossier.onOpen.AddListener(OnOpen);
            Dossier.onClose.AddListener(OnClose);
            Dossier.onSelectedVariant.AddListener(OnSelectedVariant);
            Dossier.onFailedToOpen.AddListener(OnFailedToOpen);
            Dossier.onLoadedProjectArea.AddListener(OnLoadedProjectArea);
        }

        private void OnDisable()
        {
            Dossier.onOpening.RemoveListener(OnOpening);
            Dossier.onOpen.RemoveListener(OnOpen);
            Dossier.onClose.RemoveListener(OnClose);
            Dossier.onSelectedVariant.RemoveListener(OnSelectedVariant);
            Dossier.onFailedToOpen.RemoveListener(OnFailedToOpen);
        }

        private void OnOpening()
        {
            onOpening.Invoke();
        }

        private void OnOpen(Dossier dossier)
        {
            onOpen.Invoke(dossier);
        }

        private void OnFailedToOpen()
        {
            onFailedToOpen.Invoke();
        }

        private void OnSelectedVariant(Variant? variant)
        {
            onSelectedVariant.Invoke(variant);
        }

        private void OnClose()
        {
            onClose.Invoke();
        }

        private void OnLoadedProjectArea(FeatureCollection featureCollection)
        {
            onLoadedProjectArea.Invoke(featureCollection);
        }
    }
}