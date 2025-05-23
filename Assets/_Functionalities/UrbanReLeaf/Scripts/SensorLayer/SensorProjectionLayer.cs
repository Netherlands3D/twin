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
using UnityEngine.Networking;
using System.Collections.Generic;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Functionalities.Wms;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Functionalities.UrbanReLeaf
{
    public class SensorProjectionLayer : ImageProjectionLayer
    {        
        public SensorDataController SensorDataController { get { return dataController; } private set { } }

        private SensorDataController dataController;            

        public int RenderIndex
        {
            get => renderIndex;
            set
            {
                int oldIndex = renderIndex;
                renderIndex = value;
                if (oldIndex != renderIndex)
                    UpdateDrawOrderForChildren();
            }
        }

        private int renderIndex = -1;

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

                //force the depth to be at least larger than its height to prevent z-fighting
                DecalProjector decalProjector = tile.gameObject.GetComponent<DecalProjector>();
                TextureDecalProjector textureDecalProjector = tile.gameObject.GetComponent<TextureDecalProjector>();
                if (ProjectorHeight >= decalProjector.size.z)
                    textureDecalProjector.SetSize(decalProjector.size.x, decalProjector.size.y, ProjectorMinDepth);

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
        }

        protected override void OnPreUpdateTile(Tile tile)
        {
            base.OnPreUpdateTile(tile);
            TileSensorDataController controller = tile.gameObject.GetComponent<TileSensorDataController>();
            controller.DestroySelectedHexagon();
        }

        public override void LayerToggled()
        {
            base.LayerToggled();
            if(!isEnabled)
            {
                //get current tiles
                foreach (KeyValuePair<Vector2Int, Tile> tile in tiles)
                {
                    if (tile.Value == null || tile.Value.gameObject == null)
                        continue;

                    if (tile.Value.runningCoroutine != null)
                        StopCoroutine(tile.Value.runningCoroutine);

                    TextureDecalProjector projector = tile.Value.gameObject.GetComponent<TextureDecalProjector>();
                    projector.gameObject.SetActive(false);

                    TileSensorDataController controller = tile.Value.gameObject.GetComponent<TileSensorDataController>();
                    controller.DestroySelectedHexagon();
                }
            }
        }

        private void UpdateDrawOrderForChildren()
        {
            foreach (KeyValuePair<Vector2Int, Tile> tile in tiles)
            {
                if (tile.Value == null || tile.Value.gameObject == null)
                    continue;

                TextureDecalProjector projector = tile.Value.gameObject.GetComponent<TextureDecalProjector>();
                projector.SetPriority(renderIndex);
            }
        }
    }
}
