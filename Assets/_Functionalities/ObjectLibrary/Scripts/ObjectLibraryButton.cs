using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Samplers;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.ObjectLibrary
{
    [RequireComponent(typeof(Button))]
    public class ObjectLibraryButton : MonoBehaviour
    {
        protected Button button;
        [SerializeField] protected GameObject prefab;
        private Action<Vector3> instantiationCallback;

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

        public void SetPrefab(GameObject prefab)
        {
            this.prefab = prefab;

            instantiationCallback = w => LayerGameObjectFactory.Create(w, prefab);
        }

        // for when this button should load from an Addressable
        public void SetPrefab(PrefabReference reference)
        {
            this.prefab = null;

            instantiationCallback = w => LayerGameObjectFactory.Create(w, reference);
        }
        
        protected virtual void CreateObject()
        {           
            var opticalRaycaster = FindAnyObjectByType<OpticalRaycaster>();
            if (!opticalRaycaster) return;

            var centerOfViewport = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            opticalRaycaster.GetWorldPointAsync(centerOfViewport, instantiationCallback);
        }
    }
}
