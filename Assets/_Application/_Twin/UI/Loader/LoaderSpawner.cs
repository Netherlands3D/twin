using UnityEngine;

namespace Netherlands3D.Twin.UI.Loader
{
    [RequireComponent(typeof(RectTransform))]
    public class LoaderSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject LoaderPrefab;
        private GameObject loader;

        public void Spawn()
        {
            if (loader != null)
            {
                Despawn();
            }

            loader = Instantiate(LoaderPrefab, gameObject.transform);
        }

        public void Despawn()
        {
            Destroy(loader);
            loader = null;
        }
    }
}