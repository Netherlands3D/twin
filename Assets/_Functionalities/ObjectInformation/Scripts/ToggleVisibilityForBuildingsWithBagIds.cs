using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Projects;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class ToggleVisibilityForBuildingsWithBagIds : MonoBehaviour
    {  
        public ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());
        private Dictionary<string, Color> buildingColors = new Dictionary<string, Color>();
        private List<string> bagIds = new List<string>()
        {
            "0363100012254297",
            "0363100100091868",
            "0363100012122535",
            "0363100012249777",
            "0363100012199405",
            "0363100012571047",
            "0363100012254334",
            "0363100012570739",
            "0363100012155182",
            "0363100012079990",
            "0363100100092000",
            "0363100012571663",
            "0363100012076532",
            "0363100012101986",
            "0363100012063048",
            "0363100012570880",
            "0363100012122538",
            "0363100012253591",
            "0363100012139825",
            "0363100012114502",
            "0363100012077142",
            "0363100012205758",
            "0363100012254286",
            "0363100012123051",
            "0363100012124178",
            "0363100012139268"
        };

        private void Start()
        {
            SetBuildingIdsToHide(bagIds);
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
            bagIds = ids;
            buildingColors.Clear();
            foreach (string id in bagIds)
                buildingColors.Add(id, Color.white);
        }

        public void SetBuildingColorsHidden(bool enabled)
        {
            for(int i = 0; i < buildingColors.Count; i++)
            {
                string key = buildingColors.ElementAt(i).Key;
                buildingColors[key] = enabled ? Color.clear : Color.white;
            }                

            if(enabled)
                ColorSetLayer = GeometryColorizer.InsertCustomColorSet(-1, buildingColors);
            else
                ColorSetLayer = GeometryColorizer.InsertCustomColorSet(-1, buildingColors);
        }

       
    }
}
