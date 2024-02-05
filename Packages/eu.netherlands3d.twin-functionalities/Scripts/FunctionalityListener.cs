using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Functionalities
{
    /// <summary>
    /// This class is used to listen to a Functionality and invoke events when it is enabled or disabled.
    /// It is added during runtime by EnableComponentsByFunctionality, and should not be added to a GameObject manually.
    /// </summary>
    public class FunctionalityListener : MonoBehaviour
    {
        [FormerlySerializedAs("feature")]
        public Functionality functionality;

        [HideInInspector] public UnityEvent<Functionality> OnEnableFunctionality = new ();
        [HideInInspector] public UnityEvent<Functionality> OnDisableFunctionality = new ();

        private void Awake() {
            functionality.OnEnable.AddListener(EnableFunctionality);
            functionality.OnDisable.AddListener(DisableFunctionality);
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
                EnableFunctionality();
            }
            else
            {
                DisableFunctionality();
            } 
        }

        private void EnableFunctionality()
        {
            OnEnableFunctionality.Invoke(functionality);
        }

        private void DisableFunctionality()
        {
            OnDisableFunctionality.Invoke(functionality);
        }

        private void OnDestroy()
        {
            functionality.OnEnable.RemoveListener(EnableFunctionality);
            functionality.OnDisable.RemoveListener(DisableFunctionality);
        }
    }
}