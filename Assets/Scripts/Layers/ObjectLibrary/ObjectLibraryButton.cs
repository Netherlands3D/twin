using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Button))]
    public class ObjectLibraryButton : MonoBehaviour
    {
        private Button button;
        [SerializeField] private GameObject prefab;
        // [SerializeField] private UnityEvent<GameObject> createdLibraryObject;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(CreateObject);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(CreateObject);
        }

        private void CreateObject()
        {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();
            
            var newObject = Instantiate(prefab, spawnPoint, Quaternion.identity);
            newObject.name = prefab.name;
            var layerComponent = newObject.GetComponent<ObjectLayer>();
            if (!layerComponent)
                newObject.AddComponent<ObjectLayer>();
        }
    }
}
