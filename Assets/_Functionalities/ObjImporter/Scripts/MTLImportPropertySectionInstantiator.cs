using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Functionalities.OBJImporter
{
    public class MTLImportPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private MTLImportPropertySection propertySectionPrefab; 
        public MTLImportPropertySection PropertySection { get; private set; }

        public void AddToProperties(RectTransform properties)
        {
            PropertySection = Instantiate(propertySectionPrefab, properties);
            PropertySection.ObjSpawner = GetComponent<OBJSpawner>();
        }
    }
}
