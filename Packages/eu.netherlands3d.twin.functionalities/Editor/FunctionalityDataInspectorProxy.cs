using Netherlands3D.Twin.Functionalities;
using UnityEngine;

namespace Netherlands3D.Twin.Editor
{
    public sealed class FunctionalityDataInspectorProxy : ScriptableObject
    {
        [SerializeReference] public FunctionalityData data;
    }
}