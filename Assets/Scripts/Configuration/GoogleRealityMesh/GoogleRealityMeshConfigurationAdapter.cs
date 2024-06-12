using Netherlands3D.Tiles3D;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration.GoogleRealityMesh
{
    public class GoogleRealityMeshKeyInjector : MonoBehaviour
    {
        [SerializeField] private Configuration configuration;
        [SerializeField] private Read3DTileset read3DTileset;

        private void Awake()
        {
            if(!read3DTileset)
                read3DTileset = GetComponent<Read3DTileset>();

            read3DTileset.publicKey = configuration.ApiKey;

#if UNITY_EDITOR
            read3DTileset.personalKey = configuration.ApiKey;
#endif

            read3DTileset.enabled = true;
        }
    }
}