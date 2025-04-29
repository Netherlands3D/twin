using Netherlands3D.Tilekit;

namespace Netherlands3D.Twin.Tilekit.Events
{
    public struct TilekitEventSource
    {
        public string EventChannelId { get; }

        public TileSet TileSet { get; }

        public TilekitEventSource(string eventChannelId, TileSet tileSet)
        {
            EventChannelId = eventChannelId;
            TileSet = tileSet;
        }
    }
}