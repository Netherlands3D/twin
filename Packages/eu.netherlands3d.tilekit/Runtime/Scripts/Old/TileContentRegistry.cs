using System.Collections.Generic;
using Netherlands3D.Tilekit.TileSets;
using Netherlands3D.Twin.Tilekit;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    [CreateAssetMenu(menuName = "Tilekit/TileContentRegistry")]
    public class TileContentRegistry : ScriptableObject
    {
        [SerializeField] public Dictionary<string, TileContentBehaviour> TileContentPrefabs = new Dictionary<string, TileContentBehaviour>();

        public TileContentBehaviour Spawn(string type, TileBehaviour tile, TileContent tileContent)
        {
            if (!TileContentPrefabs.TryGetValue(type, out var result)) return null;
            
            TileContentBehaviour component = Instantiate(result, tile.transform);
            component.TileContent = tileContent;
                
            return component;
        }
    }
}