using System.Globalization;
using System.Linq;
using Netherlands3D.Indicators.Dossiers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Indicators.UI
{
    public class IndicatorList : MonoBehaviour
    {
        [SerializeField] private DossierSO dossier;
        [SerializeField] private VerticalLayoutGroup indicatorList;
        [SerializeField] private GameObject indicatorListItemPrefab;

        private void OnEnable()
        {
            dossier.onSelectedProjectArea.AddListener(OnSelectedProjectArea);
            PopulateList(dossier.ActiveProjectArea);
        }

        private void OnDisable()
        {
            dossier.onSelectedProjectArea.RemoveListener(OnSelectedProjectArea);
        }

        private void OnSelectedProjectArea(ProjectArea? projectArea)
        {
            PopulateList(projectArea);
        }

        private void PopulateList(ProjectArea? projectArea)
        {
            if (!indicatorList) return;

            indicatorList.transform.ClearAllChildren();

            if (projectArea.HasValue == false) return;
            var selectedProjectArea = projectArea.Value;

            if (dossier.Data.HasValue == false) return;
            var dossierData = dossier.Data.Value;
            
            foreach (var indicatorWithValue in selectedProjectArea.indicators.Values)
            {
                InstantiateListItem(dossierData, indicatorWithValue.id, indicatorWithValue.value);
            }
        }

        private void InstantiateListItem(Dossier dossierData, string indicatorId, float indicatorValue)
        {
            var listItem = Instantiate(indicatorListItemPrefab, indicatorList.transform);

            var labelTextField = listItem.GetComponentsInChildren<TMP_Text>().First();
            var indicatorDefinition = dossierData.indicators.Find(indicator => indicator.id == indicatorId);
            labelTextField.text = indicatorDefinition.name;

            var valueTextField = listItem.GetComponentsInChildren<TMP_Text>().Last();
            valueTextField.text = indicatorValue.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
