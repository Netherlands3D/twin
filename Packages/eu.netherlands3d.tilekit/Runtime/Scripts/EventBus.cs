using System.Collections.Generic;
using Netherlands3D.Twin.Tilekit.Events;

namespace Netherlands3D.Twin.Tilekit
{
    public static class EventBus
    {
        private static readonly Dictionary<string, TilekitEventChannel> EventChannels = new();

        /// <summary>
        /// Exposes events for all channels, if you want to listen to anything: register on this.
        /// </summary>
        public static TilekitEventChannel All { get; } = new("all");

        /// <summary>
        /// Events for a single channel - such as a single tilemapper - to separate concerns and to
        /// optimize for performance because consumers to not need to verify the source of the event.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TilekitEventChannel Channel(string id)
        {
            return TryAddChannel(id);
        }

        private static TilekitEventChannel TryAddChannel(string id)
        {
            if (!EventChannels.ContainsKey(id))
            {
                EventChannels[id] = new TilekitEventChannel(id);
                All.AddListener(EventChannels[id]);
            }

            return EventChannels[id];
        }

        private static void TryRemoveChannel(string id)
        {
            if (!EventChannels.TryGetValue(id, out var eventChannel)) return;

            All.RemoveListener(eventChannel);
            EventChannels.Remove(id);
        }
    }
}