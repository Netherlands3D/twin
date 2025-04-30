using UnityEngine.Events;

namespace Netherlands3D.Twin.Tilekit.Events
{
    public class TileSetStreamEvent : UnityEvent<TileSetEventStreamContext>
    {
    }

    public class TileSetStreamEvent<T> : UnityEvent<TileSetEventStreamContext, T>
    {
    }

    public class TileSetStreamEvent<T1, T2> : UnityEvent<TileSetEventStreamContext, T1, T2>
    {
    }

    public class TileSetStreamEvent<T1, T2, T3> : UnityEvent<TileSetEventStreamContext, T1, T2, T3>
    {
    }
}