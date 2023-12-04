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

        public UnityEvent onActivate = new();
        public UnityEvent onDeactivate = new();
        public UnityEvent<Tool> onToggleInspector = new();
        
        [SerializeField] private bool activateToolOnInspectorToggle = true;

        [Header("Content")]
        [Tooltip("Prefab to show in the UI Inspector when this tool is activated")]
        [SerializeField] private GameObject inspectorPrefab;

        [Tooltip("GameObjects to spawn in the World when this tool is activated")]
        [SerializeField] private GameObject[] featurePrefabs;

        public GameObject InspectorPrefab { get => inspectorPrefab; private set => inspectorPrefab = value; }
        public GameObject[] FeaturePrefabs { get => featurePrefabs; private set => featurePrefabs = value; }
        private GameObject[] featureInstances;

        private bool open = false;
        public bool Open { get => open; set => open = value; }

        private void Awake() {
            open = false;
        }

        public void Activate()
        {
            onActivate.Invoke();
        }

        public void Deactivate()
        {
            onDeactivate.Invoke();
        }

        public GameObject[] SpawnPrefabInstances(Transform parent = null)
        {
            DestroyPrefabInstances();

            featureInstances = new GameObject[featurePrefabs.Length];
            for (int i = 0; i < featurePrefabs.Length; i++)
            {
                featureInstances[i] = Instantiate(featurePrefabs[i],parent,true);
            }
            return featureInstances;
        }
        
        /// <summary>
        /// Destroy all instances of the prefabs spawned in the world by activating this tool
        /// </summary>
        public void DestroyPrefabInstances()
        {
            if (featureInstances != null)
            {
                foreach (var instance in featureInstances)
                {
                    Destroy(instance);
                }
            }
            featureInstances = null;
        }

        /// <summary>
        /// Let inspector(s) know that this tool is opened or closed
        /// </summary>
        public void ToggleInspector(){
            Open = !Open;
            onToggleInspector.Invoke(this);

            if(!Open) DestroyPrefabInstances();

            if(activateToolOnInspectorToggle){
                if(open)
                {
                    Activate();
                }
                else
                {
                    Deactivate();
                }
            }
        }
    }
}