using UnityEngine;

namespace Netherlands3D.Credentials
{
    public class KeyVaultCoroutineRunner : MonoBehaviour
    {
        private static KeyVaultCoroutineRunner instance;

        public static KeyVaultCoroutineRunner Instance()
        {
            if (instance) return instance;

            var gameObject = new GameObject
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            DontDestroyOnLoad(gameObject);

            instance = gameObject.AddComponent<KeyVaultCoroutineRunner>();

            return instance;
        }
    }
}