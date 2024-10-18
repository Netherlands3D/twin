using System;
using System.Linq;
using Netherlands3D.Indicators.Dossiers;
using Netherlands3D.Indicators.Dossiers.DataLayers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Netherlands3D.Indicators.UI
{
    public class MapList : MonoBehaviour
    {
        [SerializeField] private DossierSO dossier;
        [SerializeField] private ToggleGroup mapLayerList;
        [SerializeField] private Toggle mapLayerListItemPrefab;

        [SerializeField] private UnityEvent<DataLayer> onActivateDataLayer;
        [SerializeField] private UnityEvent<DataLayer> onDeactivateDataLayer;

        private void OnEnable()
        {
            dossier.onSelectedVariant.AddListener(OnSelectedVariant);
            PopulateMapLayerList(dossier.ActiveVariant);
        }

        private void OnDisable()
        {
            // Make sure the overlay is cleared when this item is no longer active
            if (dossier.SelectedDataLayer.HasValue)
            {
                OnToggledMapListItem(dossier.SelectedDataLayer.Value, false);
            }

            dossier.onSelectedVariant.RemoveListener(OnSelectedVariant);
        }

        private void OnSelectedVariant(Variant? variant)
        {
            PopulateMapLayerList(variant);
        }

        private void PopulateMapLayerList(Variant? variant)
        {
            if (!mapLayerList) return;

            mapLayerList.transform.ClearAllChildren();
            if (dossier.SelectedDataLayer.HasValue)
            {
                OnToggledMapListItem(dossier.SelectedDataLayer.Value, false);
            }

            if (variant.HasValue == false) return;

            foreach (var dataLayer in variant.Value.maps)
            {
                InstantiateListItem(dataLayer.Value);
            }
        }

        private void InstantiateListItem(DataLayer dataLayer)
        {
            var listItem = Instantiate(mapLayerListItemPrefab, mapLayerList.transform);
            listItem.GetComponentInChildren<TMP_Text>().text = dataLayer.name;
            listItem.group = mapLayerList;

            listItem.onValueChanged.AddListener((toggledOn) => OnToggledMapListItem(dataLayer, toggledOn));
        }

        private void OnToggledMapListItem(DataLayer dataLayer, bool toggledOn)
        {
            DataLayer? toggledDataLayer = toggledOn ? dataLayer : null;
            dossier.SelectedDataLayer = toggledDataLayer;

            if(toggledDataLayer != null)
            {
                onActivateDataLayer.Invoke(dataLayer);
                return;
            }

            onDeactivateDataLayer.Invoke(dataLayer);
        }
    }
}
