using System.Collections.Generic;
using Netherlands3D.Twin.Tilekit.Events;

namespace Netherlands3D.Twin.Tilekit
{
    public static class EventBus
    {
        private static readonly Dictionary<string, TileSetEventStream> EventStreams = new();

        /// <summary>
        /// Exposes events for all channels, if you want to listen to anything: register on this.
        /// </summary>
        public static TileSetEventStream All { get; } = new("all");

        /// <summary>
        /// Events for a single channel - such as a single tilemapper - to separate concerns and to
        /// optimize for performance because consumers to not need to verify the source of the event.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TileSetEventStream Stream(string id)
        {
            return TryAddStream(id);
        }

        private static TileSetEventStream TryAddStream(string id)
        {
            if (!EventStreams.ContainsKey(id))
            {
                EventStreams[id] = new TileSetEventStream(id);
                All.AddListener(EventStreams[id]);
            }

            return EventStreams[id];
        }

        private static void TryRemoveStream(string id)
        {
            if (!EventStreams.TryGetValue(id, out var eventStream)) return;

            All.RemoveListener(eventStream);
            EventStreams.Remove(id);
        }
    }
}