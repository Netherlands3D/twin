using Netherlands3D.Twin.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class ATMDataController : MonoBehaviour
    {
        private const float earthRadius = 6378.137f;
        private const double equatorialCircumference = 2 * Mathf.PI * earthRadius;
        private const double log2x = 0.30102999566d;

        [Tooltip("The zoomlevels are scaled exponentially across this height range, tweak to get better tiling visuals")]
        [SerializeField] private float minHeightForZoomLevels = 200f;
        [Tooltip("The zoomlevels are scaled exponentially across this height range, tweak to get better tiling visuals")]
        [SerializeField] private float maxHeightForZoomLevels = 40_000_000f;

        private int currentYear;
        private int lastValidYear;
        public delegate void ATMDataHandler (int year);
        public event ATMDataHandler ChangeYear;        

        private List<int> yearsUnsupported = new List<int>() { 1625, 1724 };

        private Dictionary<int, string> yearUrls = new Dictionary<int, string>()
        {
            { 1985, @"https://images.huygens.knaw.nl/webmapper/maps/pw-1985/{z}/{x}/{y}.png" },
            { 1943, @"https://images.huygens.knaw.nl/webmapper/maps/pw-1943/{z}/{x}/{y}.png" },
            { 1909, @"https://images.huygens.knaw.nl/webmapper/maps/pw-1909/{z}/{x}/{y}.png" },
            { 1876, @"https://images.huygens.knaw.nl/webmapper/maps/loman/{z}/{x}/{y}.jpeg" },
            { 1724, @"https://images.huygens.knaw.nl/webmapper/maps/debroen/{z}/{x}/{y}.png" },
            { 1625, @"https://images.huygens.knaw.nl/webmapper/maps/berckenrode/{z}/{x}/{y}.png"}
        };

        private int[] yearsGeoJson = { 1802, 1853, 1870, 1876, 1909, 1920, 1943, 1985 };

        public int RoundDownYearGeoJson(int inputYear)
        {
            // Find the largest year in the array that is less than or equal to inputYear
            int result = yearsGeoJson[0];
            foreach (var year in yearsGeoJson)
            {
                if (year <= inputYear)
                {
                    result = year;
                }
                else
                {
                    break; // Stop checking once we've exceeded the inputYear
                }
            }

            return result;
        }

        public int RoundDownYearMaps(int inputYear)
        {
            // Find the largest year in the array that is less than or equal to inputYear
            int[] years = yearUrls.Keys.ToArray();
            foreach (var year in years)
            {
                if (year > inputYear) continue;
                
                // found the nearest
                return year;
            }

            return years[0];
        }

        private Dictionary<int, Vector4> yearBounds = new Dictionary<int, Vector4>()
        {
            { 1985, new Vector4(33627, 21516, 33691, 21566) },
            { 1943, new Vector4(33630, 21517, 33689, 21566) },
            { 1909, new Vector4(33628, 21516, 33691, 21565) },
            { 1876, new Vector4(33653, 21529, 33669, 21543) },
            { 1724, new Vector4(33651, 21529, 33668, 21544) },
            { 1625, new Vector4(33652, 21530, 33664, 21541) }
        };

        private Dictionary<int, Vector2Int> yearZoomBounds = new Dictionary<int, Vector2Int>()
        {
            { 1985, new Vector2Int(12 , 20) },
            { 1943, new Vector2Int(12 , 20) },
            { 1909, new Vector2Int(12 , 20) },
            { 1876, new Vector2Int(12 , 20) },
            { 1724, new Vector2Int(14 , 19) },
            { 1625, new Vector2Int(14 , 19) }
        };

        private Dictionary<int, int> atmZoomTileSizes = new Dictionary<int, int>()
        {
            { 12, 6037 },
            { 13, 3018 },
            { 14, 1509 },
            { 15, 754 },
            { 16, 377 },
            { 17, 188 },
            { 18, 94 },
            { 19, 47 },
            { 20, 23 }
        };

        private Transform cameraTransform;

        private void Awake()
        {
            cameraTransform = Camera.main.transform;
        }

        public int GetTileSizeForZoomLevel(int zoomLevel)
        {
            return atmZoomTileSizes[zoomLevel];
        }

        public (int minZoom, int maxZoom) GetZoomBounds()
        {
            int year = RoundDownYearMaps(ProjectData.Current.CurrentDateTime.Year);
            if (yearZoomBounds.ContainsKey(year))
                return (yearZoomBounds[year].x, yearZoomBounds[year].y);
            return (16, 16); //closest to default
        }

        public int CalculateZoomLevel()
        {
            Vector3 camPosition = cameraTransform.position;

            var currentYearZoomBounds = GetZoomBounds();

            float minimumZoomLevel = atmZoomTileSizes.Keys.Min();
            float maximumZoomLevel = atmZoomTileSizes.Keys.Max();
            float currentHeight = camPosition.y;
            
            var zoomLevel = maximumZoomLevel
                - (Mathf.Log(currentHeight / minHeightForZoomLevels) / Mathf.Log(maxHeightForZoomLevels / minHeightForZoomLevels))
                * (maximumZoomLevel - minimumZoomLevel);
            
            // Clamp to the min and max of this specific zoom level.
            zoomLevel = Math.Clamp(zoomLevel, currentYearZoomBounds.minZoom, currentYearZoomBounds.maxZoom);

            return (int)zoomLevel;  
        }

        public int GetZoomLayerIndex(int zoomLevel)
        {          
            var zoomBounds = GetZoomBoundsAllYears();

            return zoomLevel - zoomBounds.x;
        }

        public Vector2Int GetZoomBoundsAllYears()
        {
            int lowest = 100;
            int highest = 0;
            foreach(KeyValuePair<int, Vector2Int> v in yearZoomBounds)
            {
                if(v.Value.x < lowest)
                    lowest = v.Value.x;
                if(v.Value.y > highest)
                    highest = v.Value.y;
            }
            return new Vector2Int(lowest, highest);
        }
        
        private void Start()
        {
            UpdateYear(ProjectData.Current.CurrentDateTime);            
            ProjectData.Current.OnCurrentDateTimeChanged.AddListener(UpdateYear);
        }

        private void UpdateYear(DateTime time)
        {
            currentYear = RoundDownYearMaps(time.Year);
            if (yearUrls.ContainsKey(currentYear))
            {
                if (lastValidYear != currentYear)
                    ChangeYear?.Invoke(currentYear);
                lastValidYear = currentYear;
            }
        }

        public string GetUrl()
        {
            return yearUrls[RoundDownYearMaps(ProjectData.Current.CurrentDateTime.Year)];
        }

        private void OnDestroy()
        {
            ProjectData.Current.OnCurrentDateTimeChanged.RemoveListener(UpdateYear);
        }
    }
}
