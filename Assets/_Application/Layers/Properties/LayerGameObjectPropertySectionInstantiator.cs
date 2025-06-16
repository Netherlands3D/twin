using UnityEngine;

namespace Netherlands3D.Twin.Layers.Properties
{
    /// <summary>
    /// General-purpose property section instantiator for all property sections that only need access to the
    /// LayerGameObject with this game object. These property sections should have `PropertySectionWithLayerGameObject`
    /// as superclass.
    /// </summary>
    [RequireComponent(typeof(LayerGameObject))]
    public class LayerGameObjectPropertySectionInstantiator : MonoBehaviour, IPropertySectionInstantiator
    {
        [SerializeField] private PropertySectionWithLayerGameObject propertySectionPrefab;

        public void AddToProperties(RectTransform properties)
        {
            if (!propertySectionPrefab) return;

            var settings = Instantiate(propertySectionPrefab, properties);
            settings.LayerGameObject = GetComponent<LayerGameObject>();
        }
    }
}
