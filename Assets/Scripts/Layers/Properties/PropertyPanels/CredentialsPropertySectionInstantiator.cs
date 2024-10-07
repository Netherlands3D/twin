using System;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class CredentialsPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private bool autoApplyCredentials = false;
        [SerializeField] private CredentialsValidationPropertySection inputPropertySectionPrefab;  
        [HideInInspector] public UnityEvent<CredentialsValidationPropertySection> OnCredentialsPropertySectionInstantiated = new();
        
        public void AddToProperties(RectTransform properties)
        {
            if (!inputPropertySectionPrefab) return;

            var settings = Instantiate(inputPropertySectionPrefab, properties);

            settings.Handler = GetComponent<LayerCredentialsHandler>();
            if (settings.Handler == null)
                gameObject.AddComponent<LayerCredentialsHandler>();
            
            OnCredentialsPropertySectionInstantiated.Invoke(settings);
        }
    }
}