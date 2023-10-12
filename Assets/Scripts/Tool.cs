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
        [SerializeField] private VisualTreeAsset inspector;
        
        public bool UsesInspector => inspector != null;

        public void Activate()
        {
            onActivate.Invoke();
        }

        public void Deactivate()
        {
            onDeactivate.Invoke();
        }
    }
}