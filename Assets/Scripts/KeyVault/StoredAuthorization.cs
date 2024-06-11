namespace Netherlands3D.Twin
{
    [System.Serializable]
    public class StoredAuthorization
    {
        public string url = "";

        public string username = "";
        public string password = "";
        public string key = "";

        public AuthorizationType authorizationType = AuthorizationType.None;
    }
}