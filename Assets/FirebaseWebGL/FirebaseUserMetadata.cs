using System;

namespace FirebaseWebGL.Database
{
    [Serializable]
    public class FirebaseUserMetadata
    {
        public ulong lastSignInTimestamp;

        public ulong creationTimestamp;
    }
}
