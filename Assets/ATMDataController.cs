using Netherlands3D.Twin.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class ATMDataController : MonoBehaviour
    {
        private int currentYear;
        private int lastValidYear;
        public delegate void ATMDataHandler (int year);
        public event ATMDataHandler ChangeYear;

        //public UnityEvent<int> ChangeYear;

        //lets save the bounds of maps here
        //private static Vector4[] mapBounds;
        //private List<int> yearKeys = new List<int>();

        private Dictionary<int, string> yearUrls = new Dictionary<int, string>()
        {
            { 1985, @"https://images.huygens.knaw.nl/webmapper/maps/pw-1985/{z}/{x}/{y}.png" },
            { 1943, @"https://images.huygens.knaw.nl/webmapper/maps/pw-1943/{z}/{x}/{y}.png" },
            { 1909, @"https://images.huygens.knaw.nl/webmapper/maps/pw-1909/{z}/{x}/{y}.png" },
            { 1876, @"https://images.huygens.knaw.nl/webmapper/maps/loman/{z}/{x}/{y}.jpeg" },
            { 1724, @"https://images.huygens.knaw.nl/webmapper/maps/debroen/{z}/{x}/{y}.png" },
            { 1625, @"https://images.huygens.knaw.nl/webmapper/maps/berckenrode/{z}/{x}/{y}.png"}
        };

        private Dictionary<int, Vector4> yearBounds = new Dictionary<int, Vector4>()
        {
            { 1985, new Vector4(33627, 21516, 33691, 21566) },
            { 1943, new Vector4(33630, 21517, 33689, 21566) },
            { 1909, new Vector4(33628, 21516, 33691, 21565) },
            { 1876, new Vector4(33653, 21529, 33669, 21543) },
            { 1724, new Vector4(33651, 21529, 33668, 21544) },
            { 1625, new Vector4(33652, 21530, 33664, 21541) }
        };


        private int GetClosestMapYearToCurrentYear()
        {
            int projectYear = ProjectData.Current.CurrentDateTime.Year;
            int closestYear = -1;
            float closestDistance = float.MaxValue;
            foreach(KeyValuePair<int, string> kvp in yearUrls)
            {
                float distToYear = Mathf.Abs(kvp.Key - projectYear);
                if (distToYear < closestDistance)
                {
                    closestDistance = distToYear;
                    closestYear = kvp.Key;  
                }
            }
            return closestYear;
        }

        private void Start()
        {
            UpdateYear(ProjectData.Current.CurrentDateTime);            
            ProjectData.Current.OnCurrentDateTimeChanged.AddListener(UpdateYear);
        }

        private void UpdateYear(DateTime time)
        {
            currentYear = GetClosestMapYearToCurrentYear();
            if (yearUrls.ContainsKey(currentYear))
            {
                if (lastValidYear != currentYear)
                    ChangeYear?.Invoke(currentYear);
                lastValidYear = currentYear;
            }
        }

        public bool IsTileWithinXY(int x, int y)
        {
            if(yearBounds.ContainsKey(currentYear)) 
            {
                Vector4 bounds = yearBounds[currentYear];
                if(x >= bounds.x && x <= bounds.z && y >= bounds.y && y <= bounds.w)
                        return true;
            }
            return false;
        }

        public string GetUrl()
        {
            if (!yearUrls.ContainsKey(currentYear))
            {
                return yearUrls[lastValidYear];
            }
            else
            {
                return yearUrls[currentYear];
            }
        }

        private void OnDestroy()
        {
            ProjectData.Current.OnCurrentDateTimeChanged.RemoveListener(UpdateYear);
        }
    }
}
