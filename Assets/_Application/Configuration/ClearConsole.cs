using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
#endif

namespace Netherlands3D
{
    public class ClearConsole : MonoBehaviour
    {
        public void ClearLog()
        {
#if UNITY_EDITOR
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(this, null);
            Debug.Log("console cleared", gameObject);
#endif
        }
    }
}
