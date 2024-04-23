using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class Tile3DLayer2 : ReferencedLayer, ILayerWithProperties
    {
        private Read3DTileset tileSet;
        private List<IPropertySectionInstantiator> propertySections = new();

        public string URL
        {
            get => tileSet.tilesetUrl;
            set => tileSet.tilesetUrl = value; //todo: check if refresh is needed
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
            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
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