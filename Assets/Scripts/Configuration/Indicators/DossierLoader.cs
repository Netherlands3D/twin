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
        }

        private void OnDisable()
        {
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