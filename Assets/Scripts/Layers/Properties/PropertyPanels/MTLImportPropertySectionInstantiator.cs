using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class MTLImportPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private MTLImportPropertySection propertySectionPrefab; 
        public MTLImportPropertySection PropertySection { get; private set; }

        public void AddToProperties(RectTransform properties)
        {
            PropertySection = Instantiate(propertySectionPrefab, properties);
            PropertySection.ObjSpawner = GetComponent<ObjSpawner>();
        }
    }
}
