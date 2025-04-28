using Netherlands3D.Tilekit;

namespace Netherlands3D.Twin.Tilekit.Events
{
    public struct EventSource
    {
        public TileSet TileSet { get; }

        public EventSource(TileSet tileSet)
        {
            TileSet = tileSet;
        }
    }
}