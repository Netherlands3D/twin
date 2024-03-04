using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Functionalities
{
    /// <summary>
    /// This class is used to listen to a Functionality and invoke events when it is enabled or disabled.
    /// </summary>
    public class FunctionalityListener : MonoBehaviour
    {
        [FormerlySerializedAs("feature")]
        public Functionality functionality;

        public UnityEvent<Functionality> OnEnableFunctionality = new ();
        public UnityEvent<Functionality> OnDisableFunctionality = new ();

        private void Awake() {
            functionality.OnEnable.AddListener(EnableFunctionality);
            functionality.OnDisable.AddListener(DisableFunctionality);
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