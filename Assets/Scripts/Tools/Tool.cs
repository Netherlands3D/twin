using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(menuName = "Netherlands3D/Tool", fileName = "Tool", order = 0)]
    public class Tool : ScriptableObject
    {
        public string code;
        public string title;

        public FunctionGroup[] functionGroups;

        public UnityEvent<bool> onAvailabilityChange = new();

        [FormerlySerializedAs("onActivate")]
        public UnityEvent onOpen = new();

        [FormerlySerializedAs("onDeactivate")]
        public UnityEvent onClose = new();
        public UnityEvent<Tool> onToggleInspector = new();

        [Header("Content")]
        [Tooltip("Prefab to show in the UI Inspector when this tool is activated")]
        [SerializeField] private GameObject inspectorPrefab;

        [Tooltip("GameObjects to spawn in the World when this tool is activated")]
        [FormerlySerializedAs("featurePrefabs")]
        [SerializeField] private GameObject[] functionalityPrefabs;

        public GameObject InspectorPrefab { get => inspectorPrefab; private set => inspectorPrefab = value; }
        public GameObject[] FunctionalityPrefabs { get => functionalityPrefabs; private set => functionalityPrefabs = value; }
        private GameObject[] functionalityInstances;

        [SerializeField] private bool open = false;
        private bool available = false;

        public bool Open { 
            get{
                return open;
            }
            set{
                open = value;
                if(open)
                {
                    onOpen.Invoke();
                }
                else{
                    onClose.Invoke();
                }
            }
        }
        
        public bool Available { get => available; set => available = value; }

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

        /// <summary>
        /// Activate this tool (via menu)
        /// </summary>
        public void Activate()
        {
            onOpen.Invoke();
        }

        /// <summary>
        /// Deactivate this tool (via menu)
        /// </summary>
        public void Deactivate()
        {
            onClose.Invoke();
        }

        public GameObject[] SpawnPrefabInstances(Transform parent = null)
        {
            DestroyPrefabInstances();

            functionalityInstances = new GameObject[functionalityPrefabs.Length];
            for (int i = 0; i < functionalityPrefabs.Length; i++)
            {
                functionalityInstances[i] = Instantiate(functionalityPrefabs[i],parent,true);
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

        /// <summary>
        /// Let inspector(s) know that this tool is opened or closed
        /// </summary>
        public void ToggleInspector(){
            Open = !Open;
            onToggleInspector.Invoke(this);

            if(!Open) DestroyPrefabInstances();
        }

        public void OpenInspector()
        {
            if(Open) return;

            Open = true;
            onToggleInspector.Invoke(this);
        }

        public void CloseInspector()
        {
            if(!Open) return;

            Open = false;
            onToggleInspector.Invoke(this);

            DestroyPrefabInstances();
        }
    }
}