using System.Collections.Generic;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Tilekit
{
    public interface ITilekitEvents
    {
        UnityEvent<ITileMapper> UpdateTriggered { get; }
        UnityEvent<ITileMapper, TileSet> TileSetLoaded { get; }
        UnityEvent<ITileMapper, Plane[]> FrustumChanged { get; }
        UnityEvent<ITileMapper, Tiles> TilesSelected { get; }
        UnityEvent<ITileMapper, List<Change>> TransitionCreated { get; }
        UnityEvent<ITileMapper, Change> ChangeScheduleRequested { get; }
        UnityEvent<ITileMapper, List<Change>> ChangesScheduled { get; }
        UnityEvent<ITileMapper, Change> ChangeApply { get; }
        UnityEvent<ITileMapper, TileBehaviour> TileSpawned { get; }
    }
}