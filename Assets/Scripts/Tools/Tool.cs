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

        /// <summary>
        /// Let inspector(s) know that this tool is opened or closed
        /// </summary>
        public void ToggleInspector(){
            Open = !Open;
            onToggleInspector.Invoke(this);

            if(activateToolOnInspectorToggle){
                if(open)
                    Activate();
                else
                    Deactivate();
            }
        }
    }
}