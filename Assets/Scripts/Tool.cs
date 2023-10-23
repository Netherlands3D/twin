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
        public string icon = "pointer";
        public UnityEvent onActivate = new();
        public UnityEvent onDeactivate = new();
        [SerializeField] private VisualTreeAsset inspectorTemplate;
        
        public VisualTreeAsset InspectorTemplate => inspectorTemplate;
        public bool UsesInspector => InspectorTemplate != null;
        public VisualElement InspectorInstance { get; set; }


        public void Activate()
        {
            onActivate.Invoke();
        }

        public void Deactivate()
        {
            onDeactivate.Invoke();
        }

        // public override bool Equals(object other)
        // {
        //     if (other is not Tool)
        //         return false;
        //     
        //     return ((Tool)other).code == code;
        // }
    }
}