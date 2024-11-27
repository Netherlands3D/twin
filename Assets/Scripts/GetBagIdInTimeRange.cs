using System;
using System.Collections.Generic;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class GetBagIdInTimeRange : MonoBehaviour
    {
        [SerializeField] private TextAsset csv;
        [SerializeField] private HiddenBagIds hiddenBagIds;
        private Dictionary<string, DateTime> availableBagIdStartTimes = new();
        private ATMBagIdHider bagIdHider;

        private void Awake()
        {
            bagIdHider = GetComponent<ATMBagIdHider>();
        }

        private void OnEnable()
        {
            ProjectData.Current.OnCurrentDateTimeChanged.AddListener(UpdateBagIds);
        }
        
        private void OnDisable()
        {
            ProjectData.Current.OnCurrentDateTimeChanged.RemoveListener(UpdateBagIds);
        }

        private void UpdateBagIds(DateTime newTime)
        {
            hiddenBagIds.bagIds.Clear();
            
            foreach (var building in availableBagIdStartTimes)
            {
                if (newTime < building.Value)
                {
                    hiddenBagIds.bagIds.Add(building.Key);
                }
            }

            bagIdHider.UpdateHiddenBuildings(true);
        }

        private void Start()
        {
            var lines = CsvParser.ReadLines(csv.text, 1);

            foreach (var line in lines)
            {
                var startTime = new DateTime(int.Parse(line[1]), 1, 1);
                availableBagIdStartTimes.Add(line[0], startTime);
            }
            
            UpdateBagIds(ProjectData.Current.CurrentDateTime);
        }
    }
}