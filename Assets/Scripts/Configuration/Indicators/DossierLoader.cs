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
            configuration.OnDossierIdChanged.AddListener(OnDossierStartLoading);
        }

        private void OnDisable()
        {
            configuration.OnDossierIdChanged.RemoveListener(OnDossierStartLoading);
        }

        private void OnDossierStartLoading(string dossierId)
        {
            if (string.IsNullOrEmpty(dossierId))
            {
                dossier.Close();
                return;
            }

            if (string.IsNullOrEmpty(configuration.BaseUri) == false)
            {
                dossier.baseUri = configuration.BaseUri;
            }
            if (string.IsNullOrEmpty(configuration.ApiKey) == false)
            {
                dossier.ApiKey = configuration.ApiKey;
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