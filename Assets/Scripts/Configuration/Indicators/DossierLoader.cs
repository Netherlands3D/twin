using Netherlands3D.Indicators;
using Netherlands3D.Indicators.Dossier;
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
            dossier.onSelectedVariant.AddListener(OnSelectedVariant);
        }

        private void OnDisable()
        {
            dossier.onSelectedVariant.RemoveListener(OnSelectedVariant);
            configuration.OnDossierLoadingStart.RemoveListener(OnDossierStartLoading);
        }

        private void OnDossierStartLoading(string dossierId)
        {
            StartCoroutine(dossier.Open(dossierId));
        }

        private void OnSelectedVariant(Variant? variant)
        {
            if (variant.HasValue == false)
            {
                return;
            }

            StartCoroutine(dossier.LoadProjectAreaGeometry(variant.Value));
        }
    }
}