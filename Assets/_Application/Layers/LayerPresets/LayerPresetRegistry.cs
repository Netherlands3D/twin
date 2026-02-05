using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Netherlands3D.Twin.Layers.LayerPresets
{
    public static class LayerPresetRegistry
    {
        private static readonly Dictionary<string, Type> IdToPresetType = new();
        private static readonly Dictionary<Type, ILayerPreset> PresetTypeToInstance = new();
        private static readonly Dictionary<Type, Type> ArgsTypeToPresetType = new();

        static LayerPresetRegistry()
        {
            AutoRegisterFromAssemblies();
        }
        
        public static void Register(string kind, ILayerPreset preset)
        {
            var presetType = preset.GetType();

            IdToPresetType[kind] = presetType;
            PresetTypeToInstance[presetType] = preset;

            var argsType = presetType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILayerPreset<>))
                .Select(i => i.GetGenericArguments()[0])
                .FirstOrDefault();

            if (argsType != null)
                ArgsTypeToPresetType[argsType] = presetType;
        }

        public static ILayerBuilder Create<TPreset>(LayerPresetArgs<TPreset> args) where TPreset : ILayerPreset
        {
            var builder = new LayerBuilder();

            if (!PresetTypeToInstance.TryGetValue(typeof(TPreset), out var preset))
                return builder;

            return preset.Apply(builder, args);
        }

        public static ILayerBuilder Create(LayerPresetArgs args)
        {
            var builder = new LayerBuilder();

            if (!ArgsTypeToPresetType.TryGetValue(args.GetType(), out var presetType))
                return builder;

            return PresetTypeToInstance[presetType].Apply(builder, args);
        }

        private static void AutoRegisterFromAssemblies(params Assembly[] assemblies)
        {
            if (assemblies is not { Length: > 0 })
                assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var asm in assemblies)
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.IsAbstract) continue;
                    if (!typeof(ILayerPreset).IsAssignableFrom(type)) continue;

                    var attr = type.GetCustomAttribute<LayerPresetAttribute>();
                    if (attr == null) continue;

                    if (Activator.CreateInstance(type) is not ILayerPreset preset) continue;

                    Register(attr.Kind, preset);
                }
            }
        }
    }
}
