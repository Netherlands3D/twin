using System;

namespace FirebaseWebGL.Database
{
    [Serializable]
    public class FirebaseUserProvider
    {
        public string displayName;

        public string email;

        public string photoUrl;

        public string providerId;

        public string userId;
    }
}