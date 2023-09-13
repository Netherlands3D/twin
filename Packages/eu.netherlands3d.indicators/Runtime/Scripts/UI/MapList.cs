using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Netherlands3D.Indicators.Dossiers;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Netherlands3D.Indicators.UI
{
    public class MapList : MonoBehaviour
    {
        [SerializeField] private DossierSO dossier;
        [SerializeField] private ToggleGroup mapLayerList;
        [SerializeField] private Toggle mapLayerListItemPrefab;

        public UnityEvent<DataLayer> onSelectedMapOverlay = new();
        public UnityEvent<Uri> onLoadMapOverlayFrame = new();
        public UnityEvent onDeselectedMapOverlay = new();

        private void OnEnable()
        {
            dossier.onSelectedVariant.AddListener(OnSelectedVariant);
            PopulateMapLayerList(dossier.ActiveVariant);
        }

        private void OnDisable()
        {
            // Make sure the overlay is cleared when this item is no longer active
            onDeselectedMapOverlay.Invoke();
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
            onDeselectedMapOverlay.Invoke();

            if (variant.HasValue == false) return;

            foreach (var dataLayer in variant.Value.maps)
            {
                var listItem = Instantiate(mapLayerListItemPrefab, mapLayerList.transform);
                listItem.GetComponentInChildren<TMP_Text>().text = dataLayer.Value.name;
                listItem.group = mapLayerList;

                listItem.onValueChanged.AddListener((toggledOn) => OnToggledMapListItem(dataLayer.Value, toggledOn));
            }
        }

        private void OnToggledMapListItem(DataLayer dataLayer, bool toggledOn)
        {
            if (!toggledOn)
            {
                onDeselectedMapOverlay.Invoke();
                return;
            }

            onSelectedMapOverlay.Invoke(dataLayer);

            if (dataLayer.frames.Count == 0) return;

            // since we do not support multiple frames at the moment, we cheat and always load the first
            var firstFrame = dataLayer.frames.First();
            onLoadMapOverlayFrame.Invoke(firstFrame.map);
        }
    }
}
