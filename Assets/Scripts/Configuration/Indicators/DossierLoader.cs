using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Configuration.Indicators
{
    public class DossierLoader : MonoBehaviour
    {
        [SerializeField] private Configuration configuration;
        
        public UnityEvent<string> dossierStartLoading;

        private void OnEnable()
        {
            configuration.OnDossierLoadingStart.AddListener(OnDossierStartLoading);
        }

        private void OnDisable()
        {
            configuration.OnDossierLoadingStart.RemoveListener(OnDossierStartLoading);
        }

        private void OnDossierStartLoading(string dossierId)
        {
            dossierStartLoading.Invoke(dossierId);
        }
    }
}