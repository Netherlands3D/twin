using System.Collections.Generic;
using JetBrains.Annotations;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Tilekit
{
    public class TilekitEventSystem : MonoBehaviour, ITilekitEvents
    {
        [CanBeNull]
        public static TilekitEventSystem current { get; set; }

        // Events
        public UnityEvent<ITileMapper> UpdateTriggered { get; private set; } = new ();
        public UnityEvent<ITileMapper, TileSet> TileSetLoaded { get; private set; } = new ();
        public UnityEvent<ITileMapper, Plane[]> FrustumChanged { get; private set; } = new ();
        public UnityEvent<ITileMapper, Tiles> TilesSelected { get; private set; } = new ();
        public UnityEvent<ITileMapper, List<Change>> TransitionCreated { get; private set; } = new ();
        public UnityEvent<ITileMapper, Change> ChangeScheduleRequested { get; private set; } = new ();
        public UnityEvent<ITileMapper, List<Change>> ChangesScheduled { get; private set; } = new ();
        public UnityEvent<ITileMapper, Change> ChangeApply { get; private set; } = new ();
        public UnityEvent<ITileMapper, TileBehaviour> TileSpawned { get; private set; } = new ();

        private void Awake()
        {
            if (current == null)
            {
                current = this;
            }
            else
            {
                Destroy(this);
            }
        }
    }
}