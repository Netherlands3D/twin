using System;
using System.Collections.Generic;
using System.Reflection;

namespace Netherlands3D.Twin.Layers.LayerPresets
{
    public static class LayerPresetRegistry
    {
        private static bool isInitialized = false;
        private static readonly Dictionary<string, ILayerPreset> Presets = new();

        public static void Register(string kind, ILayerPreset preset) => Presets[kind] = preset;

        public static ILayerBuilder Create(string kind, LayerPresetArgs args)
        {
            if (!isInitialized)
            {
                AutoRegisterFromAssemblies();
            }

            var layerBuilder = new LayerBuilder();

            if (!Presets.TryGetValue(kind, out var preset)) return layerBuilder;
            
            return preset.Apply(layerBuilder, args);
        }

        public static void AutoRegisterFromAssemblies(params Assembly[] assemblies)
        {
            if (assemblies is not { Length: not 0 })
            {
                assemblies = AppDomain.CurrentDomain.GetAssemblies();
            }

            foreach (var asm in assemblies)
            {
                foreach (var type in asm.GetTypes())
                {
                    var attr = type.GetCustomAttribute<LayerPresetAttribute>();
                    if (attr == null) continue;

                    if (Activator.CreateInstance(type) is not ILayerPreset preset) continue;
                    
                    Register(attr.Kind, preset);
                }
            }
            
            isInitialized = true;
        }
    }
}