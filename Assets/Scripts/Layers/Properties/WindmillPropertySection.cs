using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class WindmillPropertySection : MonoBehaviour, IPropertySection
    {
        [SerializeField] private GameObject propertySectionPrefab;

        public void AddToProperties(Properties properties)
        {
            if (!propertySectionPrefab) return;

            Instantiate(propertySectionPrefab, transform);
        }
    }
}