using System.Linq;

namespace Netherlands3D.Twin.Layers.LayerPresets
{
    // marker base
    public abstract class LayerPresetArgs { }
    
    public abstract class LayerPresetArgs<TPreset> : LayerPresetArgs
        where TPreset : ILayerPreset { }

    public class LayerPresetResult
    {
        public LayerPresetNode[] roots;

        public static LayerPresetResult Create(LayerPresetArgs parent, LayerPresetArgs[] children = null)
        {
            return new LayerPresetResult
            {
                roots = new[]
                {
                    new LayerPresetNode
                    {
                        preset = parent, children = children?.Select(preset => new LayerPresetNode { preset = preset }).ToArray()
                    }
                }
            };
        }
        
        public static LayerPresetResult Create(LayerPresetNode root)
        {
            return new LayerPresetResult
            {
                roots = new[] { root }
            };
        }
    }

    public class LayerPresetNode
    {
        public LayerPresetArgs preset;
        public LayerPresetNode[] children;
    }
}