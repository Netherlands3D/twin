using Netherlands3D.Indicators;
using Netherlands3D.Indicators.Dossiers;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration.Indicators
{
    public class DossierLoader : MonoBehaviour
    {
        [SerializeField] private Configuration configuration;
        [SerializeField] private DossierSO dossier;

        private void OnEnable()
        {
            configuration.OnApiBaseUriChanged.AddListener(OnApiBaseUriChanged);
            configuration.OnApiKeyChanged.AddListener(OnApiKeyChanged);
            configuration.OnDossierIdChanged.AddListener(OnDossierStartLoading);
            dossier.onImport.AddListener(OnDossierImported);
        }

        private void OnDisable()
        {
            dossier.onImport.RemoveListener(OnDossierImported);
            configuration.OnDossierIdChanged.RemoveListener(OnDossierStartLoading);
            configuration.OnApiKeyChanged.RemoveListener(OnApiKeyChanged);
            configuration.OnApiBaseUriChanged.RemoveListener(OnApiBaseUriChanged);
        }

        private void OnApiBaseUriChanged(string baseUri)
        {
            dossier.baseUri = configuration.BaseUri;
        }

        private void OnApiKeyChanged(string apiKey)
        {
            dossier.ApiKey = configuration.ApiKey;
        }

        private void OnDossierImported(string dossierId)
        {
            // This will trigger the OnDossierStartLoading automatically
            configuration.DossierId = dossierId;
        }

        private void OnDossierStartLoading(string dossierId)
        {
            if (string.IsNullOrEmpty(dossierId))
            {
                dossier.Close();
                return;
            }

            StartCoroutine(dossier.Open(dossierId));
        }

        public void LoadProjectAreas(Variant? variant)
        {
            if (variant.HasValue == false)
            {
                return;
            }

            StartCoroutine(dossier.LoadProjectAreaGeometry(variant.Value));
        }
    }
}