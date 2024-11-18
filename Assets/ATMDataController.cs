using Netherlands3D.Twin.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ATMDataController : MonoBehaviour
    {
        private int currentYear;
        private int lastValidYear;
        public delegate void ATMDataHandler (int year);
        public event ATMDataHandler ChangeYear;

        private void Start()
        {
            lastValidYear = yearUrls.Keys.ElementAt(0);
            ProjectData.Current.OnCurrentDateTimeChanged.AddListener(UpdateYear);
        }

        private void UpdateYear(DateTime time)
        {
            currentYear = time.Year;
            if (yearUrls.ContainsKey(currentYear))
            {
                if (lastValidYear != currentYear)
                    ChangeYear?.Invoke(currentYear);
                lastValidYear = currentYear;
            }
        }


        private Dictionary<int, string> yearUrls = new Dictionary<int, string>()
        {
            { 1985, @"https://images.huygens.knaw.nl/webmapper/maps/pw-1943/{z}/{x}/{y}.png" },
            { 1943, @"https://images.huygens.knaw.nl/webmapper/maps/pw-1943/{z}/{x}/{y}.png" },
            { 1909, @"https://images.huygens.knaw.nl/webmapper/maps/pw-1909/{z}/{x}/{y}.png" },
            { 1876, @"https://images.huygens.knaw.nl/webmapper/maps/loman/{z}/{x}/{y}.jpeg" },
            { 1724, @"https://images.huygens.knaw.nl/webmapper/maps/debroen/{z}/{x}/{y}.png" },
            { 1625, @"https://images.huygens.knaw.nl/webmapper/maps/berckenrode/{z}/{x}/{y}.png"}
        };

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
