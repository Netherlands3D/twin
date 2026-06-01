using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Tools
{
    [CreateAssetMenu(menuName = "Netherlands3D/Tool", fileName = "Tool", order = 0)]
    public class Tool : ScriptableObject
    {
        public string code;
        public string title;

        public FunctionGroup[] functionGroups;

        public UnityEvent<bool> onAvailabilityChange = new();

        [FormerlySerializedAs("onActivate")] public UnityEvent onOpen = new();

        [FormerlySerializedAs("onDeactivate")] public UnityEvent onClose = new();
        
        public UnityEvent<GameObject> onSpawnedPrefab = new();

        [Header("Content")] [Tooltip("Prefab to show in the UI Inspector when this tool is activated")] [SerializeField]
        private GameObject inspectorPrefab;

        [SerializeField] private InspectorPanelType panelType;

        public Type PanelType => panelType.Type;
        [SerializeField] private UnityEngine.Object[] panelArgs;
        public object[] PanelArgs => panelArgs;

        [Tooltip("GameObjects to spawn in the World when this tool is activated")] [FormerlySerializedAs("featurePrefabs")] [SerializeField]
        private GameObject[] functionalityPrefabs;

        public GameObject InspectorPrefab
        {
            get => inspectorPrefab;
            private set => inspectorPrefab = value;
        }

        public GameObject[] FunctionalityPrefabs
        {
            get => functionalityPrefabs;
            private set => functionalityPrefabs = value;
        }

        private GameObject[] functionalityInstances;

        // Runtime configuration to prevent SO changes in editor
        private bool runtimeOpen = false;

        // Configuration setting, this way you can preconfigure the state of the tool
        [SerializeField] private bool open = false;

        private bool available = false;

        public bool IsOpen
        {
            get => runtimeOpen;
            private set => runtimeOpen = value;
        }

        public bool Available
        {
            get => available;
            set => available = value;
        }

        /// <summary>
        /// Set availability for the user on/off.
        /// Toolbar will show/hide the buttons for this tool.
        /// </summary>
        /// <param name="available">Set to true to show the tool button</param>
        public void SetAvailability(bool available)
        {
            Available = available;
            onAvailabilityChange.Invoke(available);
        }

        public GameObject[] SpawnPrefabInstances(Transform parent = null)
        {
            DestroyPrefabInstances();

            functionalityInstances = new GameObject[functionalityPrefabs.Length];
            for (int i = 0; i < functionalityPrefabs.Length; i++)
            {
                functionalityInstances[i] = Instantiate(functionalityPrefabs[i], parent, true);
                onSpawnedPrefab.Invoke(functionalityInstances[i]);
            }

            return functionalityInstances;
        }

        /// <summary>
        /// Destroy all instances of the prefabs spawned in the world by activating this tool
        /// </summary>
        public void DestroyPrefabInstances()
        {
            if (functionalityInstances != null)
            {
                foreach (var instance in functionalityInstances)
                {
                    Destroy(instance);
                }
            }

            functionalityInstances = null;
        }

        public void Open()
        {
            if (IsOpen) return;

            IsOpen = true;
            onOpen.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;

            IsOpen = false;
            onClose.Invoke();

            DestroyPrefabInstances();
        }
    }
}