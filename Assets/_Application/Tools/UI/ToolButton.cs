using System;
using UnityEngine;

namespace Netherlands3D.Twin.Tools.UI
{
    public class ToolButton : MonoBehaviour //todo ui toolkit: this file should be deleted
    {
        [SerializeField] private Tool tool;
        [SerializeField] private Toolbar toolbar;

        [Header("Tool button visuals")] [SerializeField]
        private GameObject enabledObjects;

        [SerializeField] private GameObject disabledObjects;

        public Tool Tool
        {
            get => tool;
            private set => tool = value;
        }

        private void OnEnable()
        {
            //use events in case the tool is opened/closed by another script to keep the visuals matching the tool state
            Tool.onOpen.AddListener(UpdateVisuals); 
            Tool.onClose.AddListener(UpdateVisuals);
        }
        
        private void OnDisable()
        {
            Tool.onOpen.RemoveListener(UpdateVisuals);
            Tool.onClose.RemoveListener(UpdateVisuals);
        }

        private void Start()
        {
            //Always start button Tool as disabled
            // Tool.Close();
        }

        private void OnValidate()
        {
            if (toolbar) return;

            toolbar = GetComponentInParent<Toolbar>();
        }

        public void Toggle()
        {
            // Tool.ToggleInspector();
            //
            // if (toolbar)
            // {
            //     toolbar.DisableOutsideToolGroup(Tool);
            // }
        }

        public void ToggleWithoutNotify(bool active, bool destroySpawnedPrefabs = false)
        {
            throw new Exception("this class is old and should not be used anymore, if you see this error, remove this class from your gameObject and use UI toolkit");
            // tool.IsOpen = active;

            if (destroySpawnedPrefabs)
            {
                tool.DestroyPrefabInstances();
            }
        }

        public void UpdateVisuals()
        {
            if (enabledObjects) enabledObjects.SetActive(Tool.IsOpen);
            if (disabledObjects) disabledObjects.SetActive(!Tool.IsOpen);
        }
    }
}