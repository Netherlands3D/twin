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
            if (nameField) nameField.text = dossier.Data.HasValue ? dossier.Data.Value.name : "";
            if (projectAreaNameField) projectAreaNameField.text = "Please select a project area.";
            dossierVisualiser.onSelectedArea.AddListener(OnSelectedArea);
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