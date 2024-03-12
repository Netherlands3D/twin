using System.Globalization;
using System.Linq;
using Netherlands3D.Indicators.Dossiers;
using Netherlands3D.Indicators.Dossiers.Indicators;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Indicators.UI
{
    public class IndicatorList : MonoBehaviour
    {
        [SerializeField] private DossierSO dossier;
        [SerializeField] private VerticalLayoutGroup indicatorList;
        [SerializeField] private HorizontalBarGraph indicatorListItemPrefab;

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
                InstantiateListItem(dossierData, indicatorWithValue.id, indicatorWithValue.value, indicatorWithValue.alertLevel);
            }
        }

        private void InstantiateListItem(Dossier dossierData, string indicatorId, float indicatorValue, IndicatorAlertLevel alertLevel)
        {
            var listItem = Instantiate(indicatorListItemPrefab, indicatorList.transform);
            var indicatorDefinition = dossierData.indicators.Find(indicator => indicator.id == indicatorId);
            listItem.name = $"{indicatorId}: {indicatorDefinition.name}";
            listItem.SetLabel(indicatorDefinition.name);
            listItem.SetValue(indicatorValue, true);
            switch (alertLevel)
            {
                case IndicatorAlertLevel.OK:
                    listItem.SetBarColor(listItem.DefaultGreen);
                    break;
                case IndicatorAlertLevel.WARNING:
                    listItem.SetBarColor(listItem.DefaultYellow);
                    break;
                case IndicatorAlertLevel.ALERT:
                    listItem.SetBarColor(listItem.DefaultRed);
                    break;
            }
        }
    }
}
