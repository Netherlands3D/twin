/*
*  Copyright (C) X Gemeente
*                X Amsterdam
*                X Economic Services Departments
*
*  Licensed under the EUPL, Version 1.2 or later (the "License");
*  You may not use this work except in compliance with the License.
*  You may obtain a copy of the License at:
*
*    https://github.com/Amsterdam/Netherlands3D/blob/main/LICENSE.txt
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" basis,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
*  implied. See the License for the specific language governing
*  permissions and limitations under the License.
*/

using System.Collections;
using UnityEngine;
using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Rendering;
using UnityEngine.Networking;
using UnityEngine.Events;
using Netherlands3D.Twin;
using System.Collections.Generic;

namespace Netherlands3D.CartesianTiles
{
    public class ImageProjectionLayer : Layer
    {
        public bool compressLoadedTextures = false;

        [SerializeField]
        private TextureProjectorBase projectorPrefab;
        public TextureProjectorBase ProjectorPrefab { get => projectorPrefab; set => projectorPrefab = value; }

        public UnityEvent<LogType, string> onLogMessage = new();
        
        public static readonly float ProjectorHeight = 100f;

        public override void HandleTile(TileChange tileChange, Action<TileChange> callback = null)
        {
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);

            switch (tileChange.action)
            {
                case TileAction.Create:
                    Tile newTile = CreateNewTile(tileKey);
                    tiles.Add(tileKey, newTile);
                    tiles[tileKey].runningCoroutine = StartCoroutine(DownloadDataAndGenerateTexture(tileChange, callback));
                    break;
                case TileAction.Upgrade:
                    tiles[tileKey].unityLOD++;
                    tiles[tileKey].runningCoroutine = StartCoroutine(DownloadDataAndGenerateTexture(tileChange, callback));
                    break;
                case TileAction.Downgrade:
                    tiles[tileKey].unityLOD--;
                    tiles[tileKey].runningCoroutine = StartCoroutine(DownloadDataAndGenerateTexture(tileChange, callback));
                    break;
                case TileAction.Remove:
                    InteruptRunningProcesses(tileKey);
                    RemoveGameObjectFromTile(tileKey);
                    tiles.Remove(tileKey); 
                    callback?.Invoke(tileChange);
                    return;
            }
        }

        protected void RemoveGameObjectFromTile(Vector2Int tileKey)
        {
            if (tiles.ContainsKey(tileKey))
            {
                Tile tile = tiles[tileKey];
                if (tile == null)
                {
                    return;
                }
                if (tile.gameObject == null)
                {
                    return;
                }

                //destroy the gameobject
                Destroy(tile.gameObject);
            }
        }

        protected virtual Tile CreateNewTile(Vector2Int tileKey)
        {
            Tile tile = new();
            tile.unityLOD = 0;
            tile.tileKey = tileKey;
            tile.layer = transform.gameObject.GetComponent<Layer>();
            tile.gameObject = Instantiate(ProjectorPrefab.gameObject);
            tile.gameObject.name = tileKey.x + "-" + tileKey.y;
            tile.gameObject.transform.parent = transform.gameObject.transform;            
            tile.gameObject.layer = tile.gameObject.transform.parent.gameObject.layer;
            Vector2Int origin = new Vector2Int(tileKey.x + (tileSize / 2), tileKey.y + (tileSize / 2));
            //Vector3 originCoordinate = CoordinateConverter.ConvertTo(origin);

            var rdCoordinate = new Coordinate(
                CoordinateSystem.RD,
                origin.x,
                origin.y,
                0.0d
            );
            var originCoordinate = CoordinateConverter.ConvertTo(rdCoordinate, CoordinateSystem.Unity).ToVector3();


            originCoordinate.y = ProjectorHeight;
            tile.gameObject.transform.position = originCoordinate; //projector is now at same position as the layer !?          
            if (tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
            {
                projector.SetSize(tileSize, tileSize, tileSize);
                projector.gameObject.SetActive(true);
            }
            return tile;
        }

        protected virtual IEnumerator DownloadDataAndGenerateTexture(TileChange tileChange, Action<TileChange> callback = null)
        {
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);

            if (!tiles.ContainsKey(tileKey))
            {
                onLogMessage.Invoke(LogType.Warning, "Tile key does not exist");
                yield break;
            }

            Tile tile = tiles[tileKey];
            string url = Datasets[tiles[tileKey].unityLOD].path;  
            
            var webRequest = UnityWebRequest.Get(url);
            tile.runningWebRequest = webRequest;
            yield return webRequest.SendWebRequest();
            tile.runningWebRequest = null;
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download {url}");
                RemoveGameObjectFromTile(tileKey);
                callback(tileChange);
            }
            else
            {
                ClearPreviousTexture(tile);               
                callback(tileChange);
            }
            yield return null;          
        }

        /// <summary>
        /// Clear existing texture from tile projector
        /// </summary>
        protected void ClearPreviousTexture(Tile tile)
        {
            if (tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
            {
                projector.ClearTexture();
            }
        }

        protected void SetProjectorTexture(Tile tile, Texture2D myTexture)
        {
            if (tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
            {
                projector.SetTexture(myTexture);
            }
            tile.gameObject.SetActive(true);
        }
    }
}
