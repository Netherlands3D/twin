using System.Collections;
using Netherlands3D.Twin.Tilekit;
using Netherlands3D.Twin.Tilekit.Events;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    /// <summary>
    /// This class is a bridge between a TileSetFactory and a TileMapper; this will ensure all events listeners have
    /// been registered before we start loading the tileset created by the provided tileSetFactory.
    ///
    /// By separating this class out, instead of this being a part of the BaseTileMapper, we can ensure the TileMapper's
    /// Start method does not need to be IENumerator and will simplify that class by separating this responsibility. 
    /// </summary>
    public class TileSetLoader : BaseTileSetProvider
    {
        [SerializeField] private BaseTileSetFactory tileSetFactory;

        protected override IEnumerator Start()
        {
            // Wait 1 frame for all eventbus subscribers to finish subscribing in their start methods, before we begin
            // emitting events as a result of loading a tileset
            yield return null;
         
            // Do it.
            TileSet = tileSetFactory.CreateTileSet();

            // Inform the rest of the system that this TileSet is now in business
            EventBus.Stream(TileSetId).TileSetLoaded.Invoke(
                new TileSetEventStreamContext(TileSetId, TileSet.Value), 
                TileSet.Value
            );
        }
    }
}
