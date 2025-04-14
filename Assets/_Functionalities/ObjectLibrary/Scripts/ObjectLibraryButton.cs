using System;
using System.Collections;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    [RequireComponent(typeof(Button))]
    public class ObjectLibraryButton : MonoBehaviour
    {
        protected Button button;
        [SerializeField] protected LayerGameObject prefab;

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

        //for when this component is created at runtime
        public void Initialize(LayerGameObject layerGameObject)
        {
            this.prefab = layerGameObject;
            
            var image = GetComponentInChildren<MatchImageToSelectionState>();
            if(layerGameObject.Thumbnail != null)
                image.SpriteState = layerGameObject.Thumbnail;
        }
        
        private void SpawnObject(Vector3 opticalSpawnPoint)
        {
            var spawnPoint = ObjectPlacementUtility.GetSpawnPoint();
            if (opticalSpawnPoint != Vector3.zero)
            {
                spawnPoint = opticalSpawnPoint;
            }

            var newObject = Instantiate(prefab, spawnPoint, prefab.transform.rotation);
            var layerComponent = newObject.GetComponent<HierarchicalObjectLayerGameObject>();
            if (!layerComponent) layerComponent = newObject.gameObject.AddComponent<HierarchicalObjectLayerGameObject>();

            layerComponent.Name = prefab.name;
        }

        protected virtual void CreateObject()
        {
            var opticalRaycaster = FindAnyObjectByType<OpticalRaycaster>();
            if (opticalRaycaster)
            {
                var centerOfViewport = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
                opticalRaycaster.GetWorldPointAsync(centerOfViewport, SpawnObject);
            }
        }
    }
}