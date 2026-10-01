namespace Netherlands3D.Twin.Layers.LayerPresets
{
    // marker base
    public abstract class LayerPresetArgs { }
    
    public abstract class LayerPresetArgs<TPreset> : LayerPresetArgs
        where TPreset : ILayerPreset { }

    public struct LayerPresetResult
    {
        public LayerPresetArgs parent;
        public LayerPresetArgs[] children;
    }
}