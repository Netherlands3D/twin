namespace Netherlands3D.Twin.Layers.LayerPresets
{
    public interface ILayerPreset
    {
        ILayerBuilder Apply(ILayerBuilder builder, LayerPresetArgs args);
    }
}