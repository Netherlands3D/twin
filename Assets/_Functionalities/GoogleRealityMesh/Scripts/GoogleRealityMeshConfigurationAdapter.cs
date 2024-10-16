using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration.GoogleRealityMesh
{
    /// <summary>
    /// The configuration adapter to apply our config parameters to the Google RealityMesh Read3DTileset.
    /// It also sets the API key as default in the credentials property section.
    /// </summary>
    public class GoogleRealityMeshConfigurationAdapter : MonoBehaviour
    {
        [SerializeField] private Configuration configuration;
        [SerializeField] private Read3DTileset read3DTileset;
        [SerializeField] private CredentialsPropertySectionInstantiator credentialsPropertySectionInstantiator;

        private void Awake()
        {
            if (!read3DTileset)
                read3DTileset = GetComponent<Read3DTileset>();

            //Apply key as default input to the credentials property section when it spawns
            
            read3DTileset.publicKey = configuration.ApiKey;
#if UNITY_EDITOR
            read3DTileset.personalKey = configuration.ApiKey;
#endif

            read3DTileset.enabled = true;
        }
    }
}