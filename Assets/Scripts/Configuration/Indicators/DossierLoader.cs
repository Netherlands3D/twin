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