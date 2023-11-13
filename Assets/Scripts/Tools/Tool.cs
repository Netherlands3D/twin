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
        [SerializeField] private GameObject inspectorPrefab;
        public GameObject InspectorPrefab { get => inspectorPrefab; private set => inspectorPrefab = value; }

        private bool open = false;
        public bool Open { get => open; set => open = value; }

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