using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class LoadATMBuildings : MonoBehaviour
    {
        [UsedImplicitly] private DataTypeChain fileImporter; // don't remove, this is used in LoadDefaultProject()
        public int[] years = { 1802, 1853, 1870, 1876, 1909, 1920, 1943 };

        private void Awake()
        {
            fileImporter = GetComponent<DataTypeChain>();
        }

        private void OnEnable()
        {
            ProjectData.Current.OnCurrentDateTimeChanged.AddListener(OnTimeChanged);
        }

        private void OnDisable()
        {
            ProjectData.Current.OnCurrentDateTimeChanged.RemoveListener(OnTimeChanged);
        }

        private void OnTimeChanged(DateTime newTime)
        {
            if (years.Contains(newTime.Year))
            {
                LoadBuildings(newTime.Year);
            }
        }

        private void LoadBuildings(int year)
        {
            var fileName = year + ".geojson";
            var filePath = Path.Combine(Application.streamingAssetsPath, fileName);
            Debug.Log("loading buildings: " + filePath);
            fileImporter.DetermineAdapter(filePath);
        }
    }
}