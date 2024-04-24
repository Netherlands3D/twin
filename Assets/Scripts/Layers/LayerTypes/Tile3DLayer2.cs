using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class Tile3DLayer2 : ReferencedLayer, ILayerWithProperties
    {
        private Read3DTileset tileSet;

        [SerializeField] private bool allowURLEditInPropertySection;
        private List<IPropertySectionInstantiator> propertySections = new();

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

        protected override void Awake()
        {
            base.Awake();
            tileSet = GetComponent<Read3DTileset>();

            if (allowURLEditInPropertySection)
                propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            else
                propertySections = new();
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
    }
}