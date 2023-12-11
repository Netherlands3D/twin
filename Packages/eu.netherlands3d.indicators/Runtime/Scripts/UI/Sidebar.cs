using Netherlands3D.Indicators.Dossiers;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Indicators.UI
{
    public class Sidebar : MonoBehaviour
    {
        [SerializeField] private DossierSO dossier;
        [SerializeField] private TMP_Text nameField;
        [SerializeField] private TMP_Text projectAreaNameField;

        private DossierVisualiser dossierVisualiser;

        private void OnEnable()
        {
            dossierVisualiser = FindObjectOfType<DossierVisualiser>();
            dossierVisualiser.onSelectedArea.AddListener(OnSelectedArea);

            if (nameField) nameField.text = dossier.Data.HasValue ? dossier.Data.Value.name : "";
            if (projectAreaNameField) projectAreaNameField.text = "Please select a project area.";
            if (dossier.ActiveProjectArea.HasValue) SetProjectAreaName(dossier.ActiveProjectArea.Value);
        }

        private void OnDisable()
        {
            dossierVisualiser.onSelectedArea.RemoveListener(OnSelectedArea);
        }

        private void OnSelectedArea(ProjectAreaVisualisation visualisation)
        {
            SetProjectAreaName(visualisation.ProjectArea);
        }

        private void SetProjectAreaName(ProjectArea projectArea)
        {
            if (projectAreaNameField) projectAreaNameField.text = projectArea.name;
        }
    }
}