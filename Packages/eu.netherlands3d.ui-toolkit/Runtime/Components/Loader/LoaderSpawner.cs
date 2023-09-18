using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.UIKit.Loader
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