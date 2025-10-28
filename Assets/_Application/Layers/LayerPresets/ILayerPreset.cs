namespace Netherlands3D.Twin.Layers.LayerPresets
{
    public interface ILayerPreset
    {
        ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args);
    }
    
    public interface ILayerPreset<in TArgs> : ILayerPreset
        where TArgs : LayerPresetArgs
    {
        ILayerBuilder Apply(ILayerBuilder builder, TArgs args);
    }
}