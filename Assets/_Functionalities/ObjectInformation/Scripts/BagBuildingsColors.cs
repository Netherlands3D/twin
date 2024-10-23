using Netherlands3D.SubObjects;
using Netherlands3D.Twin.Projects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class BagBuildingsColors
    {
        //string id = "0995100000716359";
        //Dictionary<string, Color> colorset = new Dictionary<string, Color>()
        //{
        //    { id, new Color(0,0,0,0) }
        //};

        private UnityEvent<ProjectData> onBuildingsHide = data => SetBuildingColorsHidden();

        private void Start()
        {
            ProjectData.Current.OnDataChanged.AddListener(SetBuildingColorsHidden);
        }


        public ColorSetLayer ColorSetLayer { get; private set; } = new ColorSetLayer(0, new());
        private Dictionary<string, Color> buildingColors = new Dictionary<string, Color>()
        {
            { "0363100012254297" , Color.clear },
            { "0363100100091868" , Color.clear },
            { "0363100012122535" , Color.clear },
            { "0363100012249777" , Color.clear },
            { "0363100012199405" , Color.clear },
            { "0363100012571047" , Color.clear },
            { "0363100012254334" , Color.clear },
            { "0363100012570739" , Color.clear },
            { "0363100012155182" , Color.clear },
            { "0363100012079990" , Color.clear },
            { "0363100100092000" , Color.clear },
            { "0363100012571663" , Color.clear },
            { "0363100012076532" , Color.clear },
            { "0363100012101986" , Color.clear },
            { "0363100012063048" , Color.clear },
            { "0363100012570880" , Color.clear },
            { "0363100012122538" , Color.clear },
            { "0363100012253591" , Color.clear },
            { "0363100012139825" , Color.clear },
            { "0363100012114502" , Color.clear },
            { "0363100012077142" , Color.clear },
            { "0363100012205758" , Color.clear },
            { "0363100012254286" , Color.clear },
            { "0363100012123051" , Color.clear },
            { "0363100012124178" , Color.clear },
            { "0363100012139268" , Color.clear }
        };

        public void SetBuildingIdsToHide(List<string> bagIds)
        {
            buildingColors.Clear();
            foreach (string id in bagIds)
                buildingColors.Add(id, Color.clear);
        }


        public void SetBuildingColorsHidden()
        {            
            ColorSetLayer = GeometryColorizer.InsertCustomColorSet(-1, buildingColors);
        }

       
    }
}
