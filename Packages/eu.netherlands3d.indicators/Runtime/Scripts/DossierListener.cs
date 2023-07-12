using Netherlands3D.Indicators.Dossier;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Indicators
{
    public class DossierListener : MonoBehaviour
    {
        [SerializeField] private DossierSO Dossier;
        
        public UnityEvent<Dossier.Dossier> onOpen = new();
        public UnityEvent<Dossier.Variant?> onSelectedVariant = new();
        public UnityEvent onFailedToOpen = new();
        public UnityEvent onClose = new();

        private void OnEnable()
        {
            Dossier.onOpen.AddListener(OnOpen);
            Dossier.onClose.AddListener(OnClose);
            Dossier.onSelectedVariant.AddListener(OnSelectedVariant);
            Dossier.onFailedToOpen.AddListener(OnFailedToOpen);
        }

        private void OnDisable()
        {
            Dossier.onOpen.RemoveListener(OnOpen);
            Dossier.onClose.RemoveListener(OnClose);
            Dossier.onSelectedVariant.RemoveListener(OnSelectedVariant);
            Dossier.onFailedToOpen.RemoveListener(OnFailedToOpen);
        }

        private void OnOpen(Dossier.Dossier dossier)
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
    }
}