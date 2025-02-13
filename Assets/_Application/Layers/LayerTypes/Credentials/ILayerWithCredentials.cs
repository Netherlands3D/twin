using System;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials
{
    public interface ILayerWithCredentials
    {
        public UnityEvent<UnityWebRequest> OnServerResponseReceived { get; }
        public UnityEvent<Uri> OnURLChanged { get; }

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