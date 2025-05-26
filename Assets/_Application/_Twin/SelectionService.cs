using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class SelectionService : MonoBehaviour
    {
        [SerializeField] private bool isDefaultActivatedService;
        private static List<SelectionService> allSelectionServices = new();

        private void Awake()
        {
            allSelectionServices.Add(this);
            if (isDefaultActivatedService && allSelectionServices.FirstOrDefault(s => s.isDefaultActivatedService))
            {
                throw new Exception("Only 1 selection service can be enabled when others are disabled.");
            }
        }

        private void OnEnable()
        {
            foreach (var selectionService in allSelectionServices)
            {
                if (selectionService == this)
                    continue;

                selectionService.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            foreach (var selectionService in allSelectionServices)
            {
                if (selectionService == this)
                    continue;
                
                if (selectionService.gameObject.activeSelf)
                    return;

                if (selectionService.isDefaultActivatedService)
                    selectionService.gameObject.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            allSelectionServices.Remove(this);
        }
    }
}