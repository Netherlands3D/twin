using System.Collections.Generic;
using System.Diagnostics;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    public interface ILayerWithCredentials
    {
        public CredentialsPropertySection PropertySection { get; set; }

        public void SetCredentials(string username, string password);
        public void SetKey(string key);
        public void SetToken(string token);
        public void SetCode(string code);
    }
}