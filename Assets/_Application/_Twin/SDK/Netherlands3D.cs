using Netherlands3D._Application._Twin.SDK;
using UnityEngine;

namespace Netherlands3D
{
    [RequireComponent(typeof(Layers))]
    public class Netherlands3D : MonoBehaviour
    {
        private static Netherlands3D instance;

        public static Layers Layers { get; private set; }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            
            Layers = GetComponent<Layers>();
        }
    }
}
