using Netherlands3D.SubObjects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ATMBagIdHider : MonoBehaviour
    {
        [SerializeField] private HiddenBagIds data;
        public ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());
        private Dictionary<string, Color> buildingColors = new Dictionary<string, Color>();

        private void Start()
        {
            data.bagIds.Clear();
        }

        private void OnEnable()
        {
            SetBuildingColorsHidden(true);
        }

        private void OnDisable()
        {
            SetBuildingColorsHidden(false);
        }

        public void UpdateHiddenBuildings(bool hidden)
        {
            ColorSetLayer?.ColorSet.Clear();
            SetBuildingIdsToHide(data.bagIds);
            SetBuildingColorsHidden(hidden);
        }
        
        public void SetBuildingIdsToHide(List<string> ids)
        {
            buildingColors.Clear();
            foreach (string id in ids)
                buildingColors.Add(id, new Color(0,2, 0, 0)); //for now a 2 mapping in the y channel to have the shader recognise this as the position vertex key
            ColorSetLayer = new ColorSetLayer(-2, buildingColors);
        }

        public void SetBuildingColorsHidden(bool enabled)
        {
            if (enabled)
                ColorSetLayer = GeometryColorizer.AddAndMergeCustomColorSet(-2, buildingColors);
            else
                GeometryColorizer.RemoveCustomColorSet(ColorSetLayer);
        }
    }
}