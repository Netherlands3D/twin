namespace Netherlands3D.Twin
{
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