namespace Netherlands3D.Credentials
{
    [System.Serializable]
    public class StoredAuthorization
    {
        public string url = "";

        public string username = "";
        public string password = "";
        public string key = "";

        public AuthorizationType authorizationType = AuthorizationType.Public;
    }
}