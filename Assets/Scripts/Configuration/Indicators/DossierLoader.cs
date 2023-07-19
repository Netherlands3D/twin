using Netherlands3D.Indicators;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration.Indicators
{
    public class DossierLoader : MonoBehaviour
    {
        [SerializeField] private Configuration configuration;
        [SerializeField] private DossierSO dossier;

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
            StartCoroutine(dossier.Open(dossierId));
        }

        public void LoadProjectAreas()
        {
            var variant = dossier.ActiveVariant;
            if (variant.HasValue == false)
            {
                return;
            }

            StartCoroutine(dossier.LoadProjectAreaGeometry(variant.Value));
        }
    }
}