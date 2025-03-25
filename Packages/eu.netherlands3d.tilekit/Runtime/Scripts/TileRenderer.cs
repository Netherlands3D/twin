using Netherlands3D.Tilekit.Changes;
using RSG;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public abstract class TileRenderer : ScriptableObject
    {
        public virtual Promise Add(Change change)
        {
            return Promise.Resolved() as Promise;
        }

        public virtual Promise Replace(Change change)
        {
            return Promise.Resolved() as Promise;
        }

        public virtual Promise Remove(Change change)
        {
            return Promise.Resolved() as Promise;
        }
    }
}