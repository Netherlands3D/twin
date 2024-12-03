using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Twin._Functionalities.Wms.Scripts;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ATMPointLayerSpawner : MonoBehaviour
    {
        [SerializeField] private ATMPointLayer pointLayerPrefab;
        private int currentVisibleYear;
        private ATMPointLayer visibleLayer;
        private ATMDataController atmData;

        private void Start()
        {
            //data should exist at start from atmlayermanager
            atmData = FindObjectOfType<ATMDataController>();
        }

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
        }
        
        private void OnTimeChanged(DateTime newTime)
        {
            var yearToLoad = atmData.RoundDownYearGeoJson(newTime.Year);
            if (yearToLoad != currentVisibleYear)
            {
                if(visibleLayer)
                    visibleLayer.GeoJSONLayer.DestroyLayer();
                
                visibleLayer = Instantiate(pointLayerPrefab, transform);
                visibleLayer.UpdateUri(yearToLoad.ToString());
                visibleLayer.GeoJSONLayer.Name = yearToLoad.ToString();
                currentVisibleYear = yearToLoad;
            }
        }
        
    }
}
