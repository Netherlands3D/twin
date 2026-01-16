/*
*  Copyright (C) X Gemeente
*                X Amsterdam
*                X Economic Services Departments
*
*  Licensed under the EUPL, Version 1.2 or later (the "License");
*  You may not use this work except in compliance with the License.
*  You may obtain a copy of the License at:
*
*    https://github.com/Amsterdam/3DAmsterdam/blob/master/LICENSE.txt
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" basis,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
*  implied. See the License for the specific language governing
*  permissions and limitations under the License.
*/
using System;

using System.Collections.Generic;
using UnityEngine;

using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using UnityEngine.Events;
using Netherlands3D.Twin.Cameras;

namespace Netherlands3D.CartesianTiles
{
    [AddComponentMenu("Netherlands3D/CartesianTiles/Tilehandler")]
    public class TileHandler : MonoBehaviour
    {
        /// <summary>
        /// if true, prevents all layers from updating tiles
        /// downloading data continues if already started
        /// </summary>
        public bool pauseLoading
        {
            set
            {
                foreach (Layer layer in layers)
                {
                    layer.pauseLoading = value;
                }
            }
        }
        public int maximumConcurrentDownloads = 6;

        [SerializeField]
        private bool filterByCameraFrustum = true;

        [HideInInspector]
        public List<Layer> layers = new List<Layer>();


        private List<int> tileSizes = new List<int>();
        /// <summary>
        /// contains, for each tilesize in tileSizes, al list with tilecoordinates an distance to camera
        /// X,Y is bottom-left coordinate of tile in RD (for example 121000,480000)
        /// Z is distance-squared to camera in m
        /// </summary>
        private Vector3Int[][] tileDistances = new Vector3Int[8][];
        private Vector3Int[] tileList = new Vector3Int[16];
        /// <summary>
        /// list of tilechanges, ready to be processed
        /// </summary>
        [HideInInspector]
        public List<TileChange> pendingTileChanges = new List<TileChange>();

        /// <summary>
        /// dictionary with tilechanges that are curently being processed
        /// Key:
        ///		X,Y is bottom-left coordinate of tile in RD (for example 121000,480000)
        ///		Z is the Layerindex of the tile
        /// </summary>
        private Dictionary<Vector3Int, TileChange> activeTileChanges = new Dictionary<Vector3Int, TileChange>();

        /// <summary>
        /// area that is visible
        /// X, Y is bottom-left coordinate in RD (for example 121000,480000)
        /// Z width of area(RD-X-direction) in M
        /// W length of area(RD-Y-direction) in M
        /// </summary>
        private Vector4 viewRange = new Vector4();

        /// <summary>
        /// postion of camera in RDcoordinates rounded to nearest integer
        /// </summary>
        private Vector3Int cameraPosition;

        /// <summary>
        /// The method to use to determine what LOD should be showed.
        /// Auto is the default, using distance from camera and LOD distances
        /// </summary>
        private LODCalculationMethod lodCalculationMethod = LODCalculationMethod.Auto;
        private float maxDistanceMultiplier = 1.0f;

        private Vector2Int tileKey;
        private Bounds tileBounds;
        private Plane[] cameraFrustumPlanes;
        private int startX;
        private int startY;
        private int endX;
        private int endY;

        public static int runningTileDataRequests = 0;

        private bool useRadialDistanceCheck = false; //Nicer for FPS cameras

        private int maxTileSize = 0;

        private float groundLevelClipRange = 1000;

        public UnityEvent<Layer> layerAdded = new();
        public UnityEvent<Layer> layerRemoved = new();
        
        private Camera activeCamera;

        void Start()
        {
            CameraService cameraService = ServiceLocator.GetService<CameraService>();
            SetActiveCamera(cameraService.ActiveCamera);
            cameraService.OnSwitchCamera.AddListener(SetActiveCamera);
            
            layers = GetComponentsInChildren<Layer>(false).ToList();
            if (layers.Count == 0)
            {
                Debug.Log("No active layers found in TileHandler", this.gameObject);
            }

            pauseLoading = false;
            CacheCameraFrustum();

            if (!activeCamera)
            {
                Debug.LogWarning("The TileHandler requires a camera. Make sure your scene has a camera, and it is tagged as MainCamera.");
                this.enabled = false;
            }

            if (tileSizes.Count == 0)
            {
                GetTilesizes();
            }
        }

        public void SetActiveCamera(Camera camera)
        {
            activeCamera = camera;
            if(activeCamera != null)
                this.enabled = true;
        }

        public void AddLayer(Layer layer)
        {
            layers.Add(layer);
            GetTilesizes();
            layerAdded.Invoke(layer);
        }

        public void RemoveLayer(Layer layer)
        {
            int layerIndex = layers.IndexOf(layer);

            // add all existing tiles to pending destroy
            int tilesizeIndex = tileSizes.IndexOf(layer.tileSize);
            foreach (Vector3Int tileDistance in tileDistances[tilesizeIndex])
            {
                tileKey = new Vector2Int(tileDistance.x, tileDistance.y);

                if (layer.tiles.ContainsKey(tileKey))
                {
                    TileChange tileChange = new TileChange();
                    tileChange.action = TileAction.Remove;
                    tileChange.X = tileKey.x;
                    tileChange.Y = tileKey.y;
                    tileChange.layerIndex = layerIndex;
                    tileChange.priorityScore = CalculatePriorityScore(layer.layerPriority, 0, tileDistance.z, TileAction.Remove);
                    AddTileChange(tileChange, layerIndex);
                }
            }

            InstantlyStartRemoveChanges();
            //since we want to remove a layer, we now need to update the layer indices of all pending and active changes to reflect the new indices. this is not ideal, but is a fix until we rewrite the TileKit
            for (var index = 0; index < pendingTileChanges.Count; index++)
            {
                var oldChange = pendingTileChanges[index];
                if (oldChange.layerIndex > layerIndex) //only these changes need their index updated.
                {
                    var newChange = oldChange;
                    newChange.layerIndex = oldChange.layerIndex - 1; //this change needs their index decremented by 1
                    Debug.Log("decrementing pending change with index " + oldChange.layerIndex + " to " + newChange.layerIndex);
                    pendingTileChanges[index] = newChange;
                }
            }

            foreach (var kvp in activeTileChanges.ToList())
            {
                var oldChange = kvp.Value;
                var newChange = oldChange;
                newChange.layerIndex = oldChange.layerIndex - 1; //this change needs their index decremented by 1
                Debug.Log("decrementing active change with index " + oldChange.layerIndex + " to " + newChange.layerIndex);

                activeTileChanges[kvp.Key] = newChange;
            }

            layers.Remove(layer);
            layerRemoved.Invoke(layer);
        }

        private void CacheCameraFrustum()
        {
            tileBounds = new Bounds();
            cameraFrustumPlanes = new Plane[6]
            {
                new Plane(), //Left
				new Plane(), //Right
				new Plane(), //Down
				new Plane(), //Up
				new Plane(), //Near
				new Plane(), //Far
			};
        }

        void Update()
        {
            if (layers.Count == 0)
            {
                return;
            }
            viewRange = GetViewRange();
            cameraPosition = GetRDCameraPosition();

            GetTileDistancesInView(tileSizes, viewRange, cameraPosition);

            pendingTileChanges.Clear();
            RemoveOutOfViewTiles();
            GetTileChanges();

            if (pendingTileChanges.Count == 0) return;

            //Start with all remove changes to clear resources. We to all remove actions, and stop any running tilechanges that share the same position and layerindex
            InstantlyStartRemoveChanges();

            if (activeTileChanges.Count < maximumConcurrentDownloads && pendingTileChanges.Count > 0)
            {
                TileChange highestPriorityTileChange = GetHighestPriorityTileChange();
                Vector3Int tilekey = new Vector3Int(highestPriorityTileChange.X, highestPriorityTileChange.Y, highestPriorityTileChange.layerIndex);
                
                if (activeTileChanges.TryGetValue(tilekey, out TileChange existingTileChange))
                {
                    //Change running tile changes to more important ones
                    Debug.Log("Upgrading existing");
                    if (existingTileChange.priorityScore < highestPriorityTileChange.priorityScore)
                    {
                        activeTileChanges[tilekey] = highestPriorityTileChange;
                        pendingTileChanges.Remove(highestPriorityTileChange);
                    }
                }
                else
                {
                    activeTileChanges.Add(tilekey, highestPriorityTileChange);
                    pendingTileChanges.Remove(highestPriorityTileChange);
                    layers[highestPriorityTileChange.layerIndex].HandleTile(highestPriorityTileChange, TileHandled);
                }
            }
        }

        private void InstantlyStartRemoveChanges()
        {
            var removeChanges = pendingTileChanges.Where(change => change.action == TileAction.Remove);
            //since we are modifying the collection, just take the first one and process it untill the collection is empty
            while (removeChanges.Any())
            {
                var removeChange = removeChanges.First(); 
                layers[removeChange.layerIndex].HandleTile(removeChange);
                pendingTileChanges.RemoveAt(0);

                //Abort all tilechanges with the same key
                AbortSimilarTileChanges(removeChange);
                AbortPendingSimilarTileChanges(removeChange);
            }
        }

        private void AbortSimilarTileChanges(TileChange removeChange)
        {
            var changes = activeTileChanges.Where(change => (change.Value.X == removeChange.X) && (change.Value.Y == removeChange.Y) && (change.Value.layerIndex == removeChange.layerIndex)).ToArray();
            for (int i = changes.Length - 1; i >= 0; i--)
            {
                var runningChange = changes[i];
                layers[removeChange.layerIndex].InteruptRunningProcesses(new Vector2Int(removeChange.X, removeChange.Y));
                layers[removeChange.layerIndex].HandleTile(removeChange);
                activeTileChanges.Remove(runningChange.Key);
            }
        }

        private void AbortPendingSimilarTileChanges(TileChange removeChange)
        {
            var changes = pendingTileChanges.Where(change => (change.X == removeChange.X) && (change.Y == removeChange.Y) && (change.layerIndex == removeChange.layerIndex)).ToArray();
            for (int i = changes.Length - 1; i >= 0; i--)
            {
                var runningChange = changes[i];
                layers[removeChange.layerIndex].InteruptRunningProcesses(new Vector2Int(removeChange.X, removeChange.Y));
                layers[removeChange.layerIndex].HandleTile(removeChange);
                pendingTileChanges.Remove(runningChange);
            }
        }

        public void TileHandled(TileChange handledTileChange)
        {
            activeTileChanges.Remove(new Vector3Int(handledTileChange.X, handledTileChange.Y, handledTileChange.layerIndex));
        }

        /// <summary>
        /// uses CameraExtent
        /// updates the variable viewrange
        /// updates the variable cameraPositionRD
        /// updates the variable cameraPosition
        /// </summary>
        private Vector4 GetViewRange()
        {
            Extent cameraExtent;
            if (activeCamera.transform.position.y > 20)
            {
                useRadialDistanceCheck = false;
                cameraExtent = activeCamera.GetRDExtent(activeCamera.farClipPlane + maxTileSize);
            }
            else
            {
                useRadialDistanceCheck = true;
                var cameraRD = new Coordinate(activeCamera.transform.position).Convert(CoordinateSystem.RD); ;
                cameraExtent = new Extent(
                    cameraRD.easting - groundLevelClipRange,
                    cameraRD.northing - groundLevelClipRange,
                    cameraRD.easting + groundLevelClipRange,
                    cameraRD.northing + groundLevelClipRange
                );
            }

            Vector4 viewRange = new Vector4();
            viewRange.x = (float)cameraExtent.MinX;
            viewRange.y = (float)cameraExtent.MinY;
            viewRange.z = (float)(cameraExtent.MaxX - cameraExtent.MinX);
            viewRange.w = (float)(cameraExtent.MaxY - cameraExtent.MinY);

            return viewRange;
        }

        private Vector3Int GetRDCameraPosition()
        {
            var cameraPositionRD = new Coordinate(activeCamera.transform.position).Convert(CoordinateSystem.RDNAP);
            Vector3Int cameraPosition = new Vector3Int();
            cameraPosition.x = (int)cameraPositionRD.easting;
            cameraPosition.y = (int)cameraPositionRD.northing;
            cameraPosition.z = (int)cameraPositionRD.height;

            return cameraPosition;
        }

        /// <summary>
        /// create a list of unique tilesizes used by all the layers
        /// save the list in variable tileSizes
        /// </summary>
        private void GetTilesizes()
        {
            int tilesize;
            tileSizes = new List<int>();
            if (layers.Count == 0)
            {
                return;
            }
            foreach (Layer layer in layers)
            {
                if (layer.gameObject.activeInHierarchy == false)
                {
                    continue;
                }
                if (layer.isEnabled == true)
                {
                    tilesize = layer.tileSize;
                    if (tileSizes.Contains(tilesize) == false)
                    {
                        tileSizes.Add(tilesize);
                    }
                }
            }

            if (tileSizes.Count == 0)
            {
                maxTileSize = 0;
                return;
            }
            
            maxTileSize = tileSizes.Max();
        }

        private Vector3 GetPlaneIntersection(Plane plane, Camera camera, Vector2 screenCoordinate)
        {
            Ray ray = camera.ViewportPointToRay(screenCoordinate);
            Vector3 dirNorm = ray.direction / ray.direction.y;
            Vector3 IntersectionPos = ray.origin - dirNorm * ray.origin.y;
            return IntersectionPos;
        }

        private int maxTileDistances = 0;
        private void GetTileDistancesInView(List<int> tileSizes, Vector4 viewRange, Vector3Int cameraPosition)
        {
            //Godview only frustum check
            if (filterByCameraFrustum && !useRadialDistanceCheck)
            {
                GeometryUtility.CalculateFrustumPlanes(activeCamera, cameraFrustumPlanes);
            }

            maxTileDistances = 0;
            foreach (int tileSize in tileSizes)
            {
                startX = (int)Math.Floor(viewRange.x / tileSize) * tileSize;
                startY = (int)Math.Floor(viewRange.y / tileSize) * tileSize;
                endX = (int)Math.Ceiling((viewRange.x + viewRange.z) / tileSize) * tileSize;
                endY = (int)Math.Ceiling((viewRange.y + viewRange.w) / tileSize) * tileSize;
                for (int i = 0; i < tileList.Length; i++)
                    tileList[i] = Vector3Int.zero;
                int tileListIndex = 0;
                for (int x = startX; x <= endX; x += tileSize)
                {
                    for (int y = startY; y <= endY; y += tileSize)
                    {
                        Vector3Int tileID = new Vector3Int(x, y, tileSize);
                        if (filterByCameraFrustum && !useRadialDistanceCheck)
                        {
                            tileBounds.SetMinMax(new Coordinate(CoordinateSystem.RD, x, y).ToUnity(), new Coordinate(CoordinateSystem.RD,x + tileSize, y + tileSize).ToUnity());
                            if (GeometryUtility.TestPlanesAABB(cameraFrustumPlanes, tileBounds))
                            {
                                EnsureArraySize(ref tileList, tileListIndex + 1);
                                tileList[tileListIndex] = new Vector3Int(x, y, (int)GetTileDistanceSquared(tileID, cameraPosition));
                                tileListIndex++;
                            }
                        }
                        else
                        {
                            EnsureArraySize(ref tileList, tileListIndex + 1);
                            tileList[tileListIndex] = new Vector3Int(x, y, (int)GetTileDistanceSquared(tileID, cameraPosition));
                            tileListIndex++;
                        }
                    }
                }

                tileDistances[maxTileDistances] = tileList;
                maxTileDistances++;
            }
        }

        void EnsureArraySize<T>(ref T[] array, int requiredSize)
        {
            if (array == null || array.Length < requiredSize)
            {
                int newSize = Math.Max(requiredSize, array?.Length * 2 ?? 4);
                Array.Resize(ref array, newSize);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            foreach (var tileList in tileDistances)
            {
                if (tileList != null)
                {
                    foreach (var tile in tileList)
                    {
                        if(tile != null)
                            Gizmos.DrawWireCube(new Coordinate(CoordinateSystem.RD,tile.x + 500, tile.y + 500, 0).ToUnity(), new Vector3(1000, 100, 1000));
                    }
                }
            }
        }

        private float GetTileDistanceSquared(Vector3Int tileID, Vector3Int cameraPosition)
        {
            float distance = 0;
            int centerOffset = (int)tileID.z / 2;
            Vector3Int center = new Vector3Int(tileID.x + centerOffset, tileID.y + centerOffset, 0);
            float delta = center.x - cameraPosition.x;
            distance += (delta * delta);
            delta = center.y - cameraPosition.y;
            distance += (delta * delta);
            delta = cameraPosition.z * cameraPosition.z;
            distance += (delta);

            return distance;
        }


        private void GetTileChanges()
        {
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                Layer layer = layers[layerIndex];
                if (layer.isEnabled == false) { continue; }
                int tilesizeIndex = tileSizes.IndexOf(layer.tileSize);
                foreach (Vector3Int tileDistance in tileDistances[tilesizeIndex])
                {
                    tileKey = new Vector2Int(tileDistance.x, tileDistance.y);
                    int LOD = CalculateUnityLOD(tileDistance, layer);
                    if (layer.tiles.ContainsKey(tileKey))
                    {
                        int activeLOD = layer.tiles[tileKey].unityLOD;
                        if (LOD == -1)
                        {
                            TileChange tileChange = new TileChange();
                            tileChange.action = TileAction.Remove;
                            tileChange.X = tileKey.x;
                            tileChange.Y = tileKey.y;
                            tileChange.layerIndex = layerIndex;
                            tileChange.priorityScore = CalculatePriorityScore(layer.layerPriority, 0, tileDistance.z, TileAction.Remove);
                            AddTileChange(tileChange, layerIndex);
                        }
                        else if (activeLOD > LOD)
                        {
                            TileChange tileChange = new TileChange();
                            tileChange.action = TileAction.Downgrade;
                            tileChange.X = tileKey.x;
                            tileChange.Y = tileKey.y;
                            tileChange.layerIndex = layerIndex;
                            tileChange.priorityScore = CalculatePriorityScore(layer.layerPriority, activeLOD - 1, tileDistance.z, TileAction.Downgrade);
                            AddTileChange(tileChange, layerIndex);
                        }
                        else if (activeLOD < LOD)
                        {
                            TileChange tileChange = new TileChange();
                            tileChange.action = TileAction.Upgrade;
                            tileChange.X = tileKey.x;
                            tileChange.Y = tileKey.y;
                            tileChange.layerIndex = layerIndex;
                            tileChange.priorityScore = CalculatePriorityScore(layer.layerPriority, activeLOD + 1, tileDistance.z, TileAction.Upgrade);
                            AddTileChange(tileChange, layerIndex);
                        }
                    }
                    else
                    {
                        if (LOD != -1)
                        {
                            TileChange tileChange = new TileChange();
                            tileChange.action = TileAction.Create;
                            tileChange.X = tileKey.x;
                            tileChange.Y = tileKey.y;
                            tileChange.priorityScore = CalculatePriorityScore(layer.layerPriority, 0, tileDistance.z, TileAction.Create);
                            tileChange.layerIndex = layerIndex;
                            AddTileChange(tileChange, layerIndex);
                        }
                    }
                }
            }
        }

        private void AddTileChange(TileChange tileChange, int layerIndex)
        {
            //don't add a tilechange if the tile has an active tilechange already
            Vector3Int activekey = new Vector3Int(tileChange.X, tileChange.Y, tileChange.layerIndex);
            if (activeTileChanges.ContainsKey(activekey) && tileChange.action != TileAction.Remove)
            {
                return;
            }
            bool tileIspending = false;
            for (int i = pendingTileChanges.Count - 1; i >= 0; i--)
            {
                if (pendingTileChanges[i].X == tileChange.X && pendingTileChanges[i].Y == tileChange.Y && pendingTileChanges[i].layerIndex == tileChange.layerIndex)
                {
                    tileIspending = true;
                }
            }

            //Replace running tile changes with this one if priority is higher
            if (tileIspending == false)
            {
                pendingTileChanges.Add(tileChange);
            }
        }

        private int CalculateUnityLOD(Vector3Int tiledistance, Layer layer)
        {
            int unityLod = -1;

            foreach (DataSet dataSet in layer.Datasets)
            {
                //Are we within distance
                if (dataSet.enabled && dataSet.maximumDistanceSquared * maxDistanceMultiplier > (tiledistance.z))
                {
                    if (lodCalculationMethod == LODCalculationMethod.Lod1)
                    {
                        return (layer.Datasets.Count > 2) ? 1 : 0;
                    }
                    else if (lodCalculationMethod == LODCalculationMethod.Lod2)
                    {
                        //Just use the dataset length for now (we currently have 3 LOD steps)
                        return layer.Datasets.Count - 1;
                    }
                    else
                    {
                        unityLod = layer.Datasets.IndexOf(dataSet);
                    }
                }
            }
            return unityLod;
        }

        /// <summary>
        /// Switch the LOD calculaton mode
        /// </summary>
        /// <param name="method">0=Auto, 1=Lod1, 2=Lod2</param>
        public void SetLODMode(int method = 0)
        {
            lodCalculationMethod = (LODCalculationMethod)method;
        }

        /// <summary>
        /// Set the multiplier to use to limit tile distances
        /// </summary>
        /// <param name="multiplier">Multiplier value</param>
        public void SetMaxDistanceMultiplier(float multiplier)
        {
            maxDistanceMultiplier = multiplier;
        }

        private int CalculatePriorityScore(int layerPriority, int lod, int distanceSquared, TileAction action)
        {
            float distanceFactor = ((5000f * 5000f) / distanceSquared);
            int priority = 1;
            switch (action)
            {
                case TileAction.Create:
                    priority = (int)((1 + (10 * (lod + layerPriority))) * distanceFactor);
                    break;
                case TileAction.Upgrade:
                    priority = (int)((1 + (1 * (lod + layerPriority))) * distanceFactor);
                    break;
                case TileAction.Downgrade:
                    priority = (int)((1 + (0.5 * (lod + layerPriority))) * distanceFactor);
                    break;
                case TileAction.Remove:
                    priority = int.MaxValue;
                    break;
                default:
                    break;
            }
            return priority;
        }

        private void RemoveOutOfViewTiles()
        {
            Layer layer = null;
            
            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                // create a list of tilekeys for the tiles that are within the viewrange
                layer = layers[layerIndex];
                if (layer == null)
                {
                    continue;
                }

                if (layer.gameObject.activeSelf == false) continue;
                if (layer.isEnabled == false) continue;
                
                int tilesizeIndex = tileSizes.IndexOf(layer.tileSize);
                var neededTiles = tileDistances[tilesizeIndex];


                // check for each active tile if the key is in the list of tilekeys within the viewrange
                foreach (var kvp in layer.tiles)
                {
                    bool isneeded = false;
                    for (int i = 0; i < neededTiles.Length; i++)
                    {
                        if (neededTiles[i].x == kvp.Key.x && neededTiles[i].y == kvp.Key.y)
                        {
                            isneeded = true;
                            break;
                        }
                    }
                    if (isneeded) continue;

                    
                    // if the tile is not within the viewrange, set it up for removal
                    AddTileChange(
                        new TileChange
                        {
                            action = TileAction.Remove,
                            X = kvp.Key.x,
                            Y = kvp.Key.y,
                            layerIndex = layerIndex,
                            priorityScore = int.MaxValue // set the priorityscore to maximum
                        }, 
                        layerIndex
                    );
                }

            }
        }

        private TileChange GetHighestPriorityTileChange()
        {
            TileChange highestPriorityTileChange = pendingTileChanges[0];
            float highestPriority = highestPriorityTileChange.priorityScore;

            for (int i = 1; i < pendingTileChanges.Count; i++)
            {
                if (pendingTileChanges[i].priorityScore > highestPriority)
                {
                    highestPriorityTileChange = pendingTileChanges[i];
                    highestPriority = highestPriorityTileChange.priorityScore;
                }
            }
            return highestPriorityTileChange;
        }
    }

    [Serializable]
    public enum LODCalculationMethod
    {
        Auto,
        Lod1,
        Lod2
    }
}
