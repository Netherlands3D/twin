using System;
using Netherlands3D.DataTypeAdapters;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class DebugLayerButton : MonoBehaviour
    {
        [SerializeField] private string url;
        private DataTypeChain dataTypeChain;

        private void Start()
        {
            dataTypeChain = GetComponentInParent<DataTypeChain>();
        }

        public void Spawn()
        {
            dataTypeChain.DetermineAdapter(url);
        }
    }
}