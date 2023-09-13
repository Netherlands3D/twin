using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Indicators.UI
{
    public class Sidebar : MonoBehaviour
    {
        [SerializeField] private DossierSO dossier;
        [SerializeField] private DossierVisualiser dossierVisualiser;
        [SerializeField] private TMP_Text nameField;
        [SerializeField] private TMP_Text projectAreaNameField;
        [SerializeField] private ToggleGroup mapLayerList;
        [SerializeField] private Toggle mapLayerListItemPrefab;

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
            PopulateMapLayerList();
        }

        private void PopulateMapLayerList()
        {
            if (!mapLayerList) return;

            mapLayerList.transform.ClearAllChildren();

            var activeVariant = dossier.ActiveVariant;
            if (activeVariant.HasValue == false) return;

            foreach (var dataLayer in activeVariant.Value.maps)
            {
                var listItem = Instantiate(mapLayerListItemPrefab, mapLayerList.transform);
                listItem.GetComponentInChildren<TMP_Text>().text = dataLayer.Value.name;
                listItem.group = mapLayerList;
            }
        }
    }
}