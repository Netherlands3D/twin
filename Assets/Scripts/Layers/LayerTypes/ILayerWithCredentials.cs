using System.Collections.Generic;
using System.Diagnostics;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Rendering;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    public interface ILayerWithCredentials
    {
        public CredentialsPropertySection PropertySection { get; set; }
        public UnityEvent<UnityWebRequest.Result> OnServerRequestFailed { get; }

        public string URL { get; set; } //The base URL to check the credentials for

        public void SetCredentials(string username, string password);
        public void SetKey(string key);
        public void SetToken(string token);
        public void SetCode(string code);

        public void ClearCredentials();
    }
}