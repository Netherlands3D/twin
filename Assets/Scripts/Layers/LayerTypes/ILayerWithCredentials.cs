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
        public UnityEvent<UnityWebRequest> OnServerResponseReceived { get; }
        public UnityEvent<string> OnURLChanged { get; }

        public string URL { get; set; } //The base URL to check the credentials for

        //Header based credentials
        public void SetCredentials(string username, string password);
        public void SetBearerToken(string token);

        //Url Query parameters
        public void SetKey(string key);
        public void SetToken(string token);
        public void SetCode(string code);

        public void ClearCredentials();
    }
}