using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D
{
    public interface IImportedObject
    {
        UnityEvent<GameObject> ObjectVisualized { get; }
    }
}
