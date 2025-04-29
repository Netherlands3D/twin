using UnityEngine.Events;

namespace Netherlands3D.Twin.Tilekit.Events
{
    public class TilekitEvent : UnityEvent<TilekitEventSource>
    {
    }

    public class TilekitEvent<T> : UnityEvent<TilekitEventSource, T>
    {
    }

    public class TilekitEvent<T1, T2> : UnityEvent<TilekitEventSource, T1, T2>
    {
    }

    public class TilekitEvent<T1, T2, T3> : UnityEvent<TilekitEventSource, T1, T2, T3>
    {
    }
}