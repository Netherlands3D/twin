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
        private Dictionary<string, Color> buildingColors = new();

        private void Start()
        {
            SetBuildingIdsToHide(data.bagIds);
        }

        private void OnEnable()
        {
            SetBuildingIdsToHide(data.bagIds);
        }

        private void OnDisable()
        {
            Interaction.RemoveOverrideColors(buildingColors);
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
            
            Interaction.AddOverrideColors(buildingColors);
        }
    }
}
