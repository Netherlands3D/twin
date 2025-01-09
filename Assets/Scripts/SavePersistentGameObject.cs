#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[Serializable]
public class MarkerData
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public MarkerData(GameObject obj)
    {
        name = obj.name;
        position = obj.transform.position;
        rotation = obj.transform.rotation;
        scale = obj.transform.localScale;
    }
}

[ExecuteInEditMode]
public class SavePersistentGameObject : MonoBehaviour
{
    private static List<GameObject> playModeObjects = new();
    private static List<MarkerData> markerDataList = new();

    private void Awake()
    {
        playModeObjects = new();
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            print("entering edit mode " + markerDataList.Count);
            foreach (var markerData in markerDataList)
            {
                print("pmo " + markerData.name);
                GameObject savedObject = new GameObject(markerData.name);
                savedObject.name = markerData.name;
                savedObject.transform.SetParent(transform);
                savedObject.transform.position = markerData.position;
                savedObject.transform.rotation = markerData.rotation;
                savedObject.transform.localScale = markerData.scale;
                // Duplicate the object to the scene and mark it as not being in prefab mode
                Undo.RegisterCreatedObjectUndo(savedObject, "Save Persistent GameObject");
                Debug.Log($"Saved GameObject '{savedObject.name}' to the scene after exiting Play mode.");
            }
            markerDataList.Clear();
        }
    }

    public static GameObject CreatePlayModeObject(string name = "PersistentObject")
    {
        var playModeObject = new GameObject(name);
        playModeObjects.Add(playModeObject);
        Debug.Log("Created GameObject in Play mode: " + playModeObject.name);
        return playModeObject;
    }

    private void OnApplicationQuit()
    {
        SaveObjectData(playModeObjects);
    }

    private void SaveObjectData(List<GameObject> gameObjects)
    {
        markerDataList = new();
        foreach (var go in gameObjects)
        {
            markerDataList.Add(new MarkerData(go));
        }
    }
}
#endif