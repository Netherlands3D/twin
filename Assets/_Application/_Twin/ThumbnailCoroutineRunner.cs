using UnityEngine;

namespace Netherlands3D
{
    public class ThumbnailCoroutineRunner : MonoBehaviour
    {
        private static ThumbnailCoroutineRunner instance;
        
        public static ThumbnailCoroutineRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("ThumbnailCoroutineRunner");
                    instance = go.AddComponent<ThumbnailCoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
