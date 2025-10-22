using System;

namespace Netherlands3D.Twin.Layers.LayerPresets
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class LayerPresetAttribute : Attribute
    {
        public string Kind { get; }
        public LayerPresetAttribute(string kind) => Kind = kind;
    }
}