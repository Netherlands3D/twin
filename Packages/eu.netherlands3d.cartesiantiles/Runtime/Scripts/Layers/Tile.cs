
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.CartesianTiles
{
    public class Tile
    {
        public Layer layer;
        public int unityLOD;
        public GameObject gameObject;
        public AssetBundle assetBundle;
        public Vector2Int tileKey;
        public UnityWebRequest runningWebRequest;
        public Coroutine runningCoroutine;
    }
}
