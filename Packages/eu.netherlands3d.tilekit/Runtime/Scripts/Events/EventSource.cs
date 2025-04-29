using Netherlands3D.Tilekit;

namespace Netherlands3D.Twin.Tilekit.Events
{
    public struct EventSource
    {
        public string EventChannelId { get; }

        public TileSet TileSet { get; }

        public EventSource(string eventChannelId, TileSet tileSet)
        {
            EventChannelId = eventChannelId;
            TileSet = tileSet;
        }
    }
}