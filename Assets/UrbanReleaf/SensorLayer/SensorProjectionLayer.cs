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

namespace Netherlands3D.CartesianTiles
{
    public class SensorProjectionLayer : ImageProjectionLayer
    {        
        private SensorDataController dataController;

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
                callback(tileChange);
            }
            else
            {
                dataController.ProcessDataFromJson(webRequest.downloadHandler.text);
                TileSensorDataController tileSensorData = tile.gameObject.GetComponent<TileSensorDataController>();
                tileSensorData.SetCells(tile, dataController);
                tileSensorData.UpdateTexture(tile, dataController);

                //free up memory
                tileSensorData.ClearCells();

                //when static sensor data we need to keep the cell data alive
                if(!dataController.StaticSensorData)
                    dataController.ClearCells();
              
                callback(tileChange);
            }
            yield return null;
        }

        
    }
}
