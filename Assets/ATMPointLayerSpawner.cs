using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ATMPointLayerSpawner : MonoBehaviour
    {
        [SerializeField] private ATMPointLayer pointLayerPrefab;
        public int[] years = { 1802, 1853, 1870, 1876, 1909, 1920, 1943 };
        private int currentVisibleYear;
        private ATMPointLayer visibleLayer;
        private ATMVlooienburgController vlooienburgController;

        private void OnEnable()
        {
            ProjectData.Current.OnDataChanged.AddListener(Initialize);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnDataChanged.RemoveListener(Initialize);
            ProjectData.Current.OnCurrentDateTimeChanged.RemoveListener(OnTimeChanged);
        }

        private void Initialize(ProjectData newProject)
        {
            newProject.OnCurrentDateTimeChanged.AddListener(OnTimeChanged);
            vlooienburgController = FindObjectOfType<ATMVlooienburgController>();
        }
        
        private void OnTimeChanged(DateTime newTime)
        {
            var yearToLoad = RoundDownYear(newTime.Year);
            if (yearToLoad != currentVisibleYear)
            {
                if(visibleLayer)
                    visibleLayer.GeoJSONLayer.DestroyLayer();
                
                visibleLayer = Instantiate(pointLayerPrefab, transform);
                visibleLayer.SetATMVlooienburg(vlooienburgController);
                visibleLayer.UpdateUri(yearToLoad.ToString());
                visibleLayer.GeoJSONLayer.Name = yearToLoad.ToString();
                currentVisibleYear = yearToLoad;
            }
        }
        
        public int RoundDownYear(int inputYear)
        {
            // Find the largest year in the array that is less than or equal to inputYear
            int result = years[0];
            foreach (var year in years)
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
    }
}
