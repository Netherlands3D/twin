using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Functionalities
{
    public class FunctionalityListener : MonoBehaviour
    {
        [FormerlySerializedAs("feature")]
        public Functionality functionality;

        public UnityEvent<Functionality> OnEnableFeature = new ();
        public UnityEvent<Functionality> OnDisableFeature = new ();

        private void Awake() {
            functionality.OnEnable.AddListener(EnableFeature);
            functionality.OnDisable.AddListener(DisableFeature);
        }

        private void OnValidate() {
            if(!GetComponent<EnableComponentsByFunctionality>()) {
                Debug.LogError("FunctionalityListener should only be added runtime by EnableComponentsByFunctionality", this.gameObject);
            }
        }
        
        private void OnEnable()
        {
            if (functionality.IsEnabled)
            {
                EnableFeature();
            }
            else
            {
                DisableFeature();
            } 
        }

        private void EnableFeature()
        {
            OnEnableFeature.Invoke(functionality);
        }

        private void DisableFeature()
        {
            OnDisableFeature.Invoke(functionality);
        }

        private void OnDestroy()
        {
            functionality.OnEnable.RemoveListener(EnableFeature);
            functionality.OnDisable.RemoveListener(DisableFeature);
        }
    }
}