using Netherlands3D.Credentials;
using System;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Netherlands3D.Twin.Layers.LayerTypes.Credentials
{
    public interface ILayerWithCredentials : ICredentialsObject
    {
        public UnityEvent<UnityWebRequest> OnServerResponseReceived { get; }
        public UnityEvent<Uri> OnURLChanged { get; }
    }
}