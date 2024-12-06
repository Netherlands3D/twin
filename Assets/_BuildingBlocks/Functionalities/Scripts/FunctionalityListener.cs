using System;
using UnityEditor;
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
        public Functionality[] functionalities;

        public UnityEvent<Functionality> OnEnableFunctionality = new();
        public UnityEvent<Functionality> OnDisableFunctionality = new();

        private void Awake()
        {
            //events are set in awake, and removed in OnDestroy to still match changes when this gameObject is disabled 
            foreach (var functionality in functionalities)
            {
                functionality.OnEnable.AddListener(EnableFunctionality);
                functionality.OnDisable.AddListener(DisableFunctionality);
            }
        }

        private void Start()
        {
            //Match initial functionality setting
            foreach (var functionality in functionalities)
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
        }

        private void EnableFunctionality()
        {
            foreach (var functionality in functionalities)
            {
                OnEnableFunctionality.Invoke(functionality);
            }
        }

        private void DisableFunctionality()
        {
            foreach (var functionality in functionalities)
            {
                OnDisableFunctionality.Invoke(functionality);
            }
        }

        private void OnDestroy()
        {
            foreach (var functionality in functionalities)
            {
                functionality.OnEnable.RemoveListener(EnableFunctionality);
                functionality.OnDisable.RemoveListener(DisableFunctionality);
            }
        }
    }
}