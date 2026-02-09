using System;
using Netherlands3D.SubObjects;
using System.Collections.Generic;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using UnityEngine;

namespace Netherlands3D.Twin.ObjectInformation
{
    public class BagIdHider : MonoBehaviour
    {
        [SerializeField] private HiddenBagIds data;
        [SerializeField] private HiddenBagIds alwaysHiddenData;
        //public ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());
        private Dictionary<string, Color> buildingColors = new();

        private void Awake()
        {
            Interaction.ObjectMappingCheckIn += OnCheckInMapping;
            Interaction.ObjectMappingCheckOut += OnCheckInMapping;
        }

        private void OnCheckInMapping(ObjectMapping mapping)
        {
            Interaction.AddOverrideColors(buildingColors);
            ApplyStyling();
        }

        private void Start()
        {
            SetBuildingIdsToHide(data.bagIds);
            Interaction.AddOverrideColors(buildingColors);
            ApplyStyling();
        }

        private void OnEnable()
        {
            SetBuildingIdsToHide(data.bagIds);
            Interaction.AddOverrideColors(buildingColors);
            ApplyStyling();
        }

        private void OnDisable()
        {
            Interaction.RemoveOverrideColors(buildingColors);
            ApplyStyling();
        }

        public void SetBuildingIdsToHide(List<string> ids)
        {
            ObjectSelectorService selector = ServiceLocator.GetService<ObjectSelectorService>();
            foreach (string id in buildingColors.Keys)
                selector.BlockBagId(id, false);
            buildingColors.Clear();
            foreach (string id in ids)
            {
                buildingColors.Add(id, Color.clear);
                selector.BlockBagId(id, true);
            }

            foreach (string id in alwaysHiddenData.bagIds)
            {
                buildingColors.Add(id, Color.clear);
                selector.BlockBagId(id, true);
            }
        }

        private void ApplyStyling()
        {
            CartesianTileLayerGameObject[] visualisations = FindObjectsOfType<CartesianTileLayerGameObject>();
            foreach (CartesianTileLayerGameObject visualisation in visualisations)
            {
                if(visualisation.Layer is not BinaryMeshLayer binaryMeshLayer) return;
                
                foreach (KeyValuePair<Vector2Int, ObjectMapping> kv in binaryMeshLayer.Mappings)
                {
                    Interaction.ApplyColors(buildingColors, kv.Value);
                }
            }
        }

        private void OnDestroy()
        {
            Interaction.ObjectMappingCheckIn -= OnCheckInMapping;
            Interaction.ObjectMappingCheckOut -= OnCheckInMapping;
        }
    }
}
