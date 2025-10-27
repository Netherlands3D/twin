using System;
using UnityEngine;

namespace Netherlands3D.CartesianTiles
{
    [Serializable]
    public class DataSet
    {
        public string Description;
        public string geoLOD;
        public string path;
        public string pathQuery;
        public float maximumDistance;
        [HideInInspector]
        public float maximumDistanceSquared;
        public bool enabled = true;


        public string url
        {
            get
            {
                return path + pathQuery;
            }
        }
    }
}
