using Netherlands3D.Twin.Functionalities;
using UnityEditor;
using UnityEngine;

namespace Netherlands3D.Twin.Editor
{
    [CustomEditor(typeof(Functionality), true)]
    public sealed class FunctionalityEditor : UnityEditor.Editor
    {
        private FunctionalityDataInspectorProxy proxy;
        private SerializedObject serializedProxy;

        private void OnEnable()
        {
            EnsureProxy();
        }

        private void OnDisable()
        {
            serializedProxy?.Dispose();
            serializedProxy = null;

            if (proxy != null)
            {
                DestroyImmediate(proxy);
            }

            proxy = null;
        }

        private void EnsureProxy()
        {
            if (proxy != null && serializedProxy != null)
            {
                return;
            }

            proxy = CreateInstance<FunctionalityDataInspectorProxy>();
            proxy.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;

            serializedProxy = new SerializedObject(proxy);
            RefreshProxy();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var functionality = (Functionality)target;

            if (proxy.data != functionality.Data)
            {
                RefreshProxy();
            }

            serializedProxy.Update();
            var dataProperty = serializedProxy.FindProperty("data");
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(dataProperty, new GUIContent("Current data"), true);
            
            if (EditorGUI.EndChangeCheck())
            { 
                serializedProxy.ApplyModifiedProperties();
            }
        }

        private void RefreshProxy()
        {
            var functionality = (Functionality)target;
            proxy.data = functionality.Data;
            serializedProxy = new SerializedObject(proxy);
        }
    }
}