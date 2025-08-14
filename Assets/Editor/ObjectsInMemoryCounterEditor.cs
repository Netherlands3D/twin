#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectsInMemoryCounter))]
public class WeakReferenceCounterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ObjectsInMemoryCounter counter = (ObjectsInMemoryCounter)target;

        if (GUILayout.Button("Dispose UnDisposed Tiles"))
        {
            counter.DisposeUnDisposed();
        }

        
        if (GUILayout.Button("Set debug tiles"))
        {
            counter.StoreFirstAliveTiles(10);
        }
    }
}
#endif
