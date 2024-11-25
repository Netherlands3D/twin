using Netherlands3D.SubObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class BagIdHider : MonoBehaviour
    {
        [SerializeField] private HiddenBagIds data;
        [SerializeField] private HiddenBagIds alwaysHiddenData;
        public ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());
        private Dictionary<string, Color> buildingColors = new Dictionary<string, Color>();

        private void Start()
        {
            SetBuildingIdsToHide(data.bagIds);
            SetBuildingColorsHidden(true);
        }

        private void OnEnable()
        {
            SetBuildingColorsHidden(true);
        }

        private void OnDisable()
        {
            SetBuildingColorsHidden(false);
        }

        public void SetBuildingIdsToHide(List<string> ids)
        {
            buildingColors.Clear();
            foreach (string id in ids)
                buildingColors.Add(id, Color.clear);
            foreach (string id in alwaysHiddenData.bagIds)
                buildingColors.Add(id, Color.clear);
        }

        public void SetBuildingColorsHidden(bool enabled)
        {
            if (enabled)
                ColorSetLayer = GeometryColorizer.InsertCustomColorSet(-2, buildingColors);
            else
                GeometryColorizer.RemoveCustomColorSet(ColorSetLayer);
        }
    }
}
