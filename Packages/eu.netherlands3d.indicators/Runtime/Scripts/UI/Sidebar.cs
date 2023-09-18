using TMPro;
using UnityEngine;

namespace Netherlands3D.Indicators.UI
{
    public class Sidebar : MonoBehaviour
    {
        [SerializeField] private DossierSO dossier;
        [SerializeField] private DossierVisualiser dossierVisualiser;
        [SerializeField] private TMP_Text nameField;
        [SerializeField] private TMP_Text projectAreaNameField;

        private void OnEnable()
        {
            dossierVisualiser.onSelectedArea.AddListener(OnSelectedArea);

            if (nameField) nameField.text = dossier.Data.HasValue ? dossier.Data.Value.name : "";
            if (projectAreaNameField) projectAreaNameField.text = "Please select a project area.";
        }

        private void OnDisable()
        {
            dossierVisualiser.onSelectedArea.RemoveListener(OnSelectedArea);
        }

        private void OnSelectedArea(ProjectAreaVisualisation visualisation)
        {
            if (projectAreaNameField) projectAreaNameField.text = visualisation.ProjectArea.name;
        }
    }
}