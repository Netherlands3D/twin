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
using Netherlands3D.Rendering;
using UnityEngine.Networking;
using Netherlands3D.Twin;
using System.Collections.Generic;

namespace Netherlands3D.CartesianTiles
{
    public class SensorProjectionLayer : ImageProjectionLayer
    {        
        public SensorDataController SensorDataController { get { return dataController; } private set { } }

        private SensorDataController dataController;
        private float lastUpdatedTimeStamp = 0;
        private float lastUpdatedInterval = 1f;
        private bool visibleTilesDirty = false;
        private List<TileChange> queuedChanges = new List<TileChange>();
        private WaitForSeconds wfs = new WaitForSeconds(0.5f);
        private Coroutine updateTilesRoutine = null;

        public TileSensorDataController GetTileController(Vector2Int key)
        {
            if(tiles.ContainsKey(key))
            {
                return tiles[key].gameObject.GetComponent<TileSensorDataController>();
            }
            return null;
        }

        protected override Tile CreateNewTile(Vector2Int tileKey)
        {
            Tile tile = base.CreateNewTile(tileKey);

            TileSensorDataController tileSensorData = tile.gameObject.AddComponent<TileSensorDataController>();
            if (dataController == null)
            {
                dataController = GetComponent<SensorDataController>();
            }
            if (tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
            {
                projector.SetSize(tileSize, tileSize, tileSize);
                projector.gameObject.SetActive(isEnabled);
                tileSensorData.Initialize();
                projector.SetTexture(tileSensorData.DataTexture);
            }
            return tile;
        }

        protected override IEnumerator DownloadDataAndGenerateTexture(TileChange tileChange, Action<TileChange> callback = null)
        {
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);

            if (!tiles.ContainsKey(tileKey))
            {
                onLogMessage.Invoke(LogType.Warning, "Tile key does not exist");
                yield break;
            }

            Tile tile = tiles[tileKey];
            UnityWebRequest webRequest = dataController.GetRequest(tile, Datasets[tiles[tileKey].unityLOD].path);
            tile.runningWebRequest = webRequest;
            yield return webRequest.SendWebRequest();
            tile.runningWebRequest = null;
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Could not download sensor data { webRequest.url }");
                RemoveGameObjectFromTile(tileKey);
            }
            else
            {
                dataController.ProcessDataFromJson(webRequest.downloadHandler.text);
                TileSensorDataController tileSensorData = tile.gameObject.GetComponent<TileSensorDataController>();
                tileSensorData.SetCells(tile, dataController);
                tileSensorData.UpdateTexture(tile, dataController);
                TextureDecalProjector projector = tile.gameObject.GetComponent<TextureDecalProjector>();
                projector.gameObject.SetActive(true);

                //free up memory
                tileSensorData.ClearCells();

                //when static sensor data we need to keep the cell data alive
                if(!dataController.StaticSensorData)
                    dataController.ClearCells();
            }
            if (callback != null)
                callback(tileChange);
            yield return null;
        }

        public void SetVisibleTilesDirty()
        {
            //is the update already running cancel it
            if (visibleTilesDirty && updateTilesRoutine != null)
            {                
                queuedChanges.Clear();
                StopCoroutine(updateTilesRoutine);
            }

            lastUpdatedTimeStamp = Time.time;
            visibleTilesDirty = true;
            updateTilesRoutine = StartCoroutine(UpdateVisibleTiles());
        }   
        
        private IEnumerator UpdateVisibleTiles()
        {
            //get current tiles
            foreach (KeyValuePair<Vector2Int, Tile> tile in tiles)
            {
                if (tile.Value == null || tile.Value.gameObject == null)
                    continue;

                TextureDecalProjector projector = tile.Value.gameObject.GetComponent<TextureDecalProjector>();
                projector.gameObject.SetActive(false);

                TileChange tileChange = new TileChange();
                tileChange.X = tile.Key.x;
                tileChange.Y = tile.Key.y;
                queuedChanges.Add(tileChange);
            }
            bool ready = true;
            while (queuedChanges.Count > 0)
            {
                //lets wait half a second in case a slider is moving
                if (Time.time - lastUpdatedTimeStamp > lastUpdatedInterval && ready)
                {
                    ready = false;
                    TileChange next = queuedChanges[0];
                    queuedChanges.RemoveAt(0);
                    Vector2Int key = new Vector2Int(next.X, next.Y);
                    if (tiles.ContainsKey(key))
                    {
                        tiles[key].runningCoroutine = StartCoroutine(DownloadDataAndGenerateTexture(next, key =>
                        {
                            ready = true;
                        }));
                    }
                    else
                    {
                        ready = true;
                    }
                }
                yield return wfs;
            }           
            
            updateTilesRoutine = null;
            visibleTilesDirty = false;
        }
    }
}
