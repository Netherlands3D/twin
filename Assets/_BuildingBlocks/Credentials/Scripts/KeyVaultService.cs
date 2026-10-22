using UnityEngine;

namespace Netherlands3D.Credentials
{
    public class KeyVaultService : MonoBehaviour
    {
        public KeyVault KeyVault => keyVault;
        
        [Tooltip("KeyVault Scriptable Object")] [SerializeField]
        private KeyVault keyVault;
    }
}
