using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class Tile3DLayer2 : ReferencedLayer, ILayerWithProperties, ILayerWithCredentials
    {
        private Read3DTileset tileSet;
        [SerializeField] private bool allowURLEditInPropertySection;
        private List<IPropertySectionInstantiator> propertySections = new();
        public UnityEvent<string> UnsupportedExtensionsMessage;
        public UnityEvent<UnityWebRequest.Result> OnServerRequestFailed { get => tileSet.OnServerRequestFailed;  }
        
        public string URL
        {
            get => tileSet.tilesetUrl;
            set
            {
                if (tileSet.tilesetUrl != value)
                {
                    tileSet.tilesetUrl = value;
                    tileSet.RefreshTiles();
                }
            }
        }

        public override bool IsActiveInScene
        {
            get => gameObject.activeSelf;
            set
            {
                gameObject.SetActive(value);
                ReferencedProxy.UI.MarkLayerUIAsDirty();
            }
        }

        private CredentialsPropertySection propertySection;
        public CredentialsPropertySection PropertySection {
            get => propertySection;
            set => propertySection = value;
        }

        protected override void Awake()
        {
            base.Awake();
            tileSet = GetComponent<Read3DTileset>();
            
            if (allowURLEditInPropertySection)
                propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            else
                propertySections = new();
        }

        private void OnEnable()
        {
            tileSet.unsupportedExtensionsParsed.AddListener(InvokeUnsupportedExtensionsMessage);
        }

        private void OnDisable()
        {
            tileSet.unsupportedExtensionsParsed.RemoveListener(InvokeUnsupportedExtensionsMessage);
        }

        private void InvokeUnsupportedExtensionsMessage(string[] unsupportedExtensions)
        {
            if (unsupportedExtensions.Length == 0)
                return;

            string message = name + " contains the following unsupported extensions: ";
            foreach (var extension in unsupportedExtensions)
            {
                message += "\n"+ extension;
            }
            UnsupportedExtensionsMessage.Invoke(message);
        }

        private IEnumerator Start()
        {
            yield return null; //wait for UI to initialize
            ReferencedProxy.UI.ToggleProperties(true);
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
        }

        public void SetCredentials(string username, string password)
        {
            tileSet.AddCustomHeader("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(username + ":" + password)), true);
            tileSet.RefreshTiles();
        }

        public void SetKey(string key)
        {
            tileSet.publicKey = key;
            tileSet.QueryKeyName = "key";
            tileSet.RefreshTiles();
        }

        public void SetToken(string token)
        {
            tileSet.AddCustomHeader("Authorization", "Bearer " + token);
            tileSet.RefreshTiles();
        }
        
        public void SetCode(string code)
        {
            tileSet.publicKey = code;
            tileSet.QueryKeyName = "code";
            tileSet.RefreshTiles();
        }

        public void ClearCredentials()
        {
            tileSet.publicKey = "";
            tileSet.QueryKeyName = "key";
            tileSet.RefreshTiles();
        }
    }
}