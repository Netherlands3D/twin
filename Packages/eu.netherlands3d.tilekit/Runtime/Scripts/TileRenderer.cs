using System;
using System.IO;
using KindMen.Uxios;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Tilekit.Changes;
using Netherlands3D.Tilekit.TileSets;
using RSG;
using Unity.Collections;
using UnityEngine;

namespace Netherlands3D.Tilekit
{
    public struct TileContentData
    {
        public Tile Tile;
        private int tileContentIndex;
        public TileContent TileContent => Tile.TileContents[tileContentIndex];
        public FileInfo FileInfo;

        public TileContentData(Tile tile, int tileContentIndex, FileInfo fileInfo)
        {
            this.Tile = tile;
            this.tileContentIndex = tileContentIndex;
            this.FileInfo = fileInfo;
        }
    }

    public class TileContentLoader
    {
        private Uxios httpClient;
        
        public TileContentLoader(StoredAuthorization storedAuthorization)
        {
            // By instantiating a new instance of Uxios with a custom config, we have a template for all requests done
            // using this TileContentLoader; meaning that authentication is applied.
            
            var config = Config.Default();
            // storedAuthorization.basedOnConfig(config);
            this.httpClient = new Uxios(config);
        }

        public IPromise<TileContentData> Load(Tile tile, int tileContentIndex)
        {
            // TODO: Can we somehow remove this currying?
            var tileContent = tile.TileContents[tileContentIndex];
            var uri = tileContent.UriTemplate.ConvertToString();
    
            return this.httpClient
                .Get<FileInfo>(new Uri(uri))
                .Then((IResponse response) =>
                {
                    return new TileContentData(tile, tileContentIndex, response.Data as FileInfo);
                });
        }
    }
    
    public abstract class TileContentDataSpawner : ScriptableObject
    {
        public abstract bool Supports(TileContentData data);

        public abstract Promise Spawn(TileContentData data); // Uses the TileContentData received from the TileContentLoader
    }

    public abstract class TileRenderer : ScriptableObject
    {
        public virtual Promise Add(Change change)
        {
            return Promise.Resolved() as Promise;
        }

        public virtual Promise Remove(Change change)
        {
            return Promise.Resolved() as Promise;
        }
    }
}