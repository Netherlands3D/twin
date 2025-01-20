using System.Collections;
using Netherlands3D.Twin;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    [RequireComponent(typeof(Button))]
    public class ObjectLibraryButton : MonoBehaviour
    {
        protected Button button;
        [SerializeField] protected GameObject prefab;

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
        public void Initialize(GameObject prefab)
        {
            this.prefab = prefab;
        }
        
        protected virtual void CreateObject()
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
                    var opticalSpawnPoint = opticalRaycaster.GetWorldPointAtCameraScreenPoint(Camera.main, centerOfViewport);
                    if (opticalSpawnPoint != Vector3.zero)
                    {
                        spawnPoint = opticalSpawnPoint;
                    }
                }
            }
            
            var newObject = Instantiate(prefab, spawnPoint, prefab.transform.rotation);
            var layerComponent = newObject.GetComponent<HierarchicalObjectLayerGameObject>();
            if (!layerComponent)
                layerComponent = newObject.AddComponent<HierarchicalObjectLayerGameObject>();
            
            layerComponent.Name = prefab.name;

            yield return null;
        }
    }
}
