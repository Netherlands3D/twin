using Netherlands3D.Credentials;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties
{

    public interface ICredentialInterface
    {
        public ICredentialHandler Handler { get; set; }
    }
}
