using System;
using System.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class BaseTileSetProvider : MonoBehaviour, ITileSetProvider
    {
        public string TileSetId { get; protected set;  } = Guid.NewGuid().ToString();
        public TileSet? TileSet { get; protected set; }
        protected abstract IEnumerator Start();
    }
}