using System;
using System.Linq;
using Netherlands3D.Indicators.Dossiers;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
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

        public UnityEvent<DataLayer> onSelectedMapOverlay = new();
        [FormerlySerializedAs("onLoadMapOverlay")] public UnityEvent<Uri> onLoadMapOverlayFrame = new();
        public UnityEvent onDeselectedMapOverlay = new();
        
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
            onDeselectedMapOverlay.Invoke();

            var activeVariant = dossier.ActiveVariant;
            if (activeVariant.HasValue == false) return;

            foreach (var dataLayer in activeVariant.Value.maps)
            {
                var listItem = Instantiate(mapLayerListItemPrefab, mapLayerList.transform);
                listItem.GetComponentInChildren<TMP_Text>().text = dataLayer.Value.name;
                listItem.group = mapLayerList;
                listItem.onValueChanged.AddListener(toggledOn =>
                {
                    if (toggledOn)
                    {
                        onSelectedMapOverlay.Invoke(dataLayer.Value);

                        if (dataLayer.Value.frames.Count == 0) return;

                        // since we do not support multiple frames at the moment, we cheat and always load the first
                        var firstFrame = dataLayer.Value.frames.First();
                        onLoadMapOverlayFrame.Invoke(firstFrame.map);

                        return;
                    }

                    onDeselectedMapOverlay.Invoke();
                });
            }
        }
    }
}