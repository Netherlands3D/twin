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
            StartCoroutine(CreateObjectCoroutine());
        }

        private IEnumerator CreateObjectCoroutine()
        {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();            

            var opticalRaycaster = FindAnyObjectByType<OpticalRaycaster>();
            if(opticalRaycaster)
            {
                //Retrieve spawnpoint from optical raycaster a few frames in a row to make sure the depth texture is updated
                var frames = 3;
                var centerOfViewport = new Vector3(Screen.width * 0.5f, Screen.height / 2, 0);
                for (int i = 0; i < frames; i++)
                {
                    yield return new WaitForEndOfFrame();
                    spawnPoint = opticalRaycaster.GetWorldPointAtCameraScreenPoint(Camera.main, centerOfViewport);
                }
            }


            var newObject = Instantiate(prefab, spawnPoint, Quaternion.identity);
            newObject.name = prefab.name;
            var layerComponent = newObject.GetComponent<HierarchicalObjectLayer>();
            if (!layerComponent)
                newObject.AddComponent<HierarchicalObjectLayer>();

            yield return null;
        }
    }
}
