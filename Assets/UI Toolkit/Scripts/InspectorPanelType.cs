using System;
using UnityEngine;
 
/// <summary>
/// Mark any class with this attribute to make it appear in the InspectorPanelType dropdown.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class InspectorPanelAttribute : Attribute { }
 
/// <summary>
/// Serializable field type that holds a reference to any class tagged with [InspectorPanel].
/// </summary>
[Serializable]
public class InspectorPanelType : ISerializationCallbackReceiver
{
    [SerializeField] private string _assemblyQualifiedName;
 
    private Type _resolvedType;
 
    public Type Type
    {
        get => _resolvedType;
        set
        {
            AssertIsPanel(value);
            _resolvedType = value;
            _assemblyQualifiedName = value?.AssemblyQualifiedName;
        }
    }
    
    public void OnBeforeSerialize()
    {
        _assemblyQualifiedName = _resolvedType?.AssemblyQualifiedName;
    }
 
    public void OnAfterDeserialize()
    {
        if (string.IsNullOrEmpty(_assemblyQualifiedName))
        {
            _resolvedType = null;
            return;
        }
 
        _resolvedType = Type.GetType(_assemblyQualifiedName);
 
        if (_resolvedType == null)
            Debug.LogWarning($"[InspectorPanelType] Could not resolve type '{_assemblyQualifiedName}'. Was the class renamed or deleted?");
    }
    
    private static void AssertIsPanel(Type t)
    {
        if (t == null) return;
        if (t.GetCustomAttributes(typeof(InspectorPanelAttribute), false).Length == 0)
            throw new ArgumentException($"Type '{t.FullName}' is not marked with [InspectorPanel].");
    }
 
    public override string ToString() => _resolvedType?.Name ?? "(None)";
}