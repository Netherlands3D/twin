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
        [SerializeField] private GameObject inputPropertySectionPrefab;
        [HideInInspector] public UnityEvent<GameObject> OnCredentialsPropertySectionInstantiated = new();

        public void AddToProperties(RectTransform properties)
        {
            if (!inputPropertySectionPrefab) return;

            var settings = Instantiate(inputPropertySectionPrefab, properties);
            var handler = GetComponent<LayerCredentialsHandler>();
            
            foreach (var credentialInterface in settings.GetComponentsInChildren<ILayerCredentialInterface>(true))
            {
                credentialInterface.Handler = handler;
            }
            
            OnCredentialsPropertySectionInstantiated.Invoke(settings);
        }
    }
}