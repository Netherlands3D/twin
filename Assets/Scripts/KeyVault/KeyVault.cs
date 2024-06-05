using UnityEngine;
using System.Collections.Generic;

namespace Netherlands3D.Twin
{
    public enum CredentialType
    {
        None = -1,
        UsernamePassword = 0,
        KeyTokenOrCode = 1,
        Key = 2,
        Token = 3,
        Code = 4,
    }

    [CreateAssetMenu(fileName = "KeyVault", menuName = "ScriptableObjects/KeyVault", order = 1)]
    public class KeyVault : ScriptableObject
    {
        [TextArea(3, 10)]
        public string Description = "";
        public List<StoredCredentials> storedCredentials = new List<StoredCredentials>();
        public List<KnownUrlCredentialType> knownUrlCredentialTypes = new List<KnownUrlCredentialType>()
        {
            new KnownUrlCredentialType() { baseUrl = "https://tile.googleapis.com/v1/3dtiles/root.json", credentialType = CredentialType.Key },
            new KnownUrlCredentialType() { baseUrl = "https://api.pdok.nl/kadaster/3d-basisvoorziening/ogc/v1_0/collections/gebouwen/3dtiles/tileset.json", credentialType = CredentialType.None }
        };

        public CredentialType DetermineCredentialType(string url)
        {
            // Check if the url is known, like Google Maps or PDOK
            foreach (var knownUrlCredentialType in knownUrlCredentialTypes)
            {
                if (url.StartsWith(knownUrlCredentialType.baseUrl))
                {
                    return knownUrlCredentialType.credentialType;
                }
            }

            // Check our own saved credentials.
            foreach (var storedCredential in storedCredentials)
            {
                if (url.StartsWith(storedCredential.url))
                {
                    return storedCredential.credentialType;
                }
            }

            return CredentialType.None;
        }

        [System.Serializable]
        public class KnownUrlCredentialType
        {
            public string baseUrl;
            public CredentialType credentialType;
        }

        [System.Serializable]
        public class StoredCredentials
        {
            public string url = "";

            public string username = "";
            public string password = "";
            public string key = "";

            public CredentialType credentialType = CredentialType.None;
        }
    }
    
}