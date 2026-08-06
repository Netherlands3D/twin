using Netherlands3D.Coordinates;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Tiles3D
{
   [System.Serializable]
    public class Tile : IDisposable
    {
       

        //implicit tiling properties
        public int level;
        public int X;
        public int Y;
        public bool hascontent;
        public Read3DTileset tileSet;
        //webtileprioritizer properties
        public int priority = 0;
        public int childrenCountDelayingDispose = 0;

        //BoundingVolume properties
        internal bool boundsAvailable = false;
        private Bounds unityBounds = new Bounds();
        public BoundingVolume boundingVolume;
        public Coordinate BottomLeft;
        public Coordinate TopRight;
        bool boundsAreValid = true;

        // load and dispose properties

        public bool inView = false;
        public bool requestedDispose = false;
        public bool requestedUpdate = false;
        internal bool nestedTilesLoaded = false;
        public bool isLoading = false;

        // tiletree properties
        public Tile parent;
        [SerializeField] public List<Tile> children = new List<Tile>();
        

        //tileproperties
        
        public TileTransform tileTransform = TileTransform.Identity();
        public double geometricError;
        public float screenSpaceError = float.MaxValue;
        public string refine;
        public string contentUri = "";
        public Content content; //Gltf content

        public int CountLoadingChildren()
        {
            int result = 0;
            if (refine == "ADD")
            {
                return 0;
            }
            foreach (var childTile in children)
            {
                if (childTile.content != null)
                {
                    if (childTile.contentUri.Contains(".json") == false)
                    {
                        if (childTile.content.State != Content.ContentLoadState.DOWNLOADED)
                        {
                            result += 1;
                        }

                    }
                }
            }
           
            
            return result;
        }
        public int loadedChildren;
        public int CountLoadedChildren()
        {
            int result = 0;
            if (refine=="ADD")
            {
                return 0;
            }
            foreach (var childTile in children)
            {
                if (childTile.content != null)
                {
                    if (childTile.contentUri.Contains(".json") == false)
                    {

                        if (childTile.content.State != Content.ContentLoadState.DOWNLOADING)
                        {
                            result++;
                        }

                    }
                }
            }
                foreach (var childTile in children)
                {
                    result += childTile.CountLoadedChildren();
                }
            loadedChildren = result;
            return result;
        }

        public int CountLoadedParents()
        {
            if (refine == "ADD")
            {
                return 1;
            }
            int result = 0;
            if (parent !=null)
            {
                if (parent.content != null)
                {
                    if (parent.contentUri.Contains(".json") == false)
                    {
                        if (parent.content.State == Content.ContentLoadState.DOWNLOADED)
                        {
                            result = 1;
                        }
                    }
                }
            }
           
            if (parent !=null)
            {
                return result + parent.CountLoadedParents();
            }
            return result;
        }

        public int CountLoadingParents()
        {
            int result = 0;
            if (parent != null)
            {
                if (parent.isLoading)
                {
                    if (parent.contentUri.Contains(".json") == false)
                    {
                        if (parent.content != null)
                        {
                            if (parent.content.State != Content.ContentLoadState.DOWNLOADED)
                            {
                                result = 1;
                            }
                        }
                    }
                }
            }
            if (parent != null)
            {
                return result + parent.CountLoadingParents();
            }
            return result;
        }

        public Bounds ContentBounds
        {
            get
            {
                return unityBounds;
            }
            set => unityBounds = value;
        }

        




        public int GetNestingDepth()
        {
            int maxDepth = 1;
            foreach (var child in children)
            {
                int depth = child.GetNestingDepth() + 1;
                if (depth > maxDepth) maxDepth = depth;

            }
            return maxDepth;
        }

        public enum TileStatus
        {
            unloaded,
            loaded
        }



        public bool IsInViewFrustrum(Camera ofCamera)
        {
            if (!boundsAvailable && boundsAreValid)
            {
                if (boundingVolume.values.Length>0)
                {
                    CalculateUnitBounds();
                }
                else
                {
                    inView = false ;
                }
                
            }
            if (boundsAvailable)
            {
                inView = false;
                if (IsPointInbounds(new Coordinate(ofCamera.transform.position).Convert(tileSet.contentCoordinateSystem),8000d))
                {
                    inView= ofCamera.InView(unityBounds);
                }
                
            }
            
            return inView;
        }

        bool IsPointInbounds(Coordinate point, double margin)
        {
            if (point.PointsLength > 2)
            {
                if (point.value3 + margin < BottomLeft.value3)
                {
                    return false;
                }

                if (point.value3 - margin > TopRight.value3)
                {
                    return false;
                }
            }

            if (point.value1 + margin < BottomLeft.value1)
            {
                return false;
            }
            if (point.value2 + margin < BottomLeft.value2)
            {
                return false;
            }
            if (point.value1 - margin > TopRight.value1)
            {
                return false;
            }
            if (point.value2 - margin > TopRight.value2)
            {
                return false;
            }
            return true;
        }

        public void CalculateUnitBounds()
        {
            if (boundingVolume == null || boundingVolume.values.Length == 0)
            {
                boundsAreValid = false;
                return;
            }

            boundsAvailable = true;
            switch (boundingVolume.boundingVolumeType)
            {
                case BoundingVolumeType.Box:

                    Coordinate boxCenterEcef = new Coordinate(tileSet.contentCoordinateSystem, boundingVolume.values[0], boundingVolume.values[1], boundingVolume.values[2]);

                    Coordinate Xaxis = new Coordinate(tileSet.contentCoordinateSystem, boundingVolume.values[3], boundingVolume.values[4], boundingVolume.values[5]);
                    Coordinate Yaxis = new Coordinate(tileSet.contentCoordinateSystem, boundingVolume.values[6], boundingVolume.values[7], boundingVolume.values[8]);
                    Coordinate Zaxis = new Coordinate(tileSet.contentCoordinateSystem, boundingVolume.values[9], boundingVolume.values[10], boundingVolume.values[11]);

                    


                    unityBounds = new Bounds();
                    unityBounds.center = boxCenterEcef.ToUnity();

                    unityBounds.Encapsulate((boxCenterEcef + Xaxis + Yaxis + Zaxis).ToUnity());
                    unityBounds.Encapsulate((boxCenterEcef + Xaxis + Yaxis - Zaxis).ToUnity());
                    unityBounds.Encapsulate((boxCenterEcef + Xaxis - Yaxis + Zaxis).ToUnity());
                    unityBounds.Encapsulate((boxCenterEcef + Xaxis - Yaxis - Zaxis).ToUnity());
                    
                    unityBounds.Encapsulate((boxCenterEcef - Xaxis + Yaxis + Zaxis).ToUnity());
                    unityBounds.Encapsulate((boxCenterEcef - Xaxis - Yaxis + Zaxis).ToUnity());

                    unityBounds.Encapsulate((boxCenterEcef - Xaxis + Yaxis - Zaxis).ToUnity());
                    unityBounds.Encapsulate((boxCenterEcef - Xaxis - Yaxis - Zaxis).ToUnity());


                    double deltaX =  Math.Abs(Xaxis.value1) + Math.Abs(Yaxis.value1) + Math.Abs(Zaxis.value1);
                    double deltaY = Math.Abs(Xaxis.value2) + Math.Abs(Yaxis.value2) + Math.Abs(Zaxis.value2);
                    double deltaZ = Math.Abs(Xaxis.value3) + Math.Abs(Yaxis.value3) + Math.Abs(Zaxis.value3);
                    BottomLeft = new Coordinate(tileSet.contentCoordinateSystem, boxCenterEcef.value1-deltaX, boxCenterEcef.value2 - deltaY, boxCenterEcef.value3 - deltaZ);
                    TopRight = new Coordinate(tileSet.contentCoordinateSystem, boxCenterEcef.value1 + deltaX, boxCenterEcef.value2 + deltaY, boxCenterEcef.value3 + deltaZ);


                    break;
                case BoundingVolumeType.Sphere:
                    var sphereRadius = boundingVolume.values[0];
                    var sphereCentre = new Coordinate(tileSet.contentCoordinateSystem, boundingVolume.values[0], boundingVolume.values[1], boundingVolume.values[2]).ToUnity();
                    var sphereMin = new Coordinate(tileSet.contentCoordinateSystem, boundingVolume.values[0]- sphereRadius, boundingVolume.values[1] - sphereRadius, boundingVolume.values[2] - sphereRadius).ToUnity();
                    var sphereMax = new Coordinate(tileSet.contentCoordinateSystem,boundingVolume.values[0]+ sphereRadius, boundingVolume.values[1]+ sphereRadius, boundingVolume.values[2]+ sphereRadius).ToUnity();
                    unityBounds.size = Vector3.zero;
                    unityBounds.center = sphereCentre;
                    unityBounds.Encapsulate(sphereMin);
                    unityBounds.Encapsulate(sphereMax);
                    BottomLeft = new Coordinate(tileSet.contentCoordinateSystem, boundingVolume.values[0] - sphereRadius, boundingVolume.values[1] - sphereRadius, boundingVolume.values[2] - sphereRadius);
                    TopRight = new Coordinate(tileSet.contentCoordinateSystem, boundingVolume.values[0] + sphereRadius, boundingVolume.values[1] + sphereRadius, boundingVolume.values[2] + sphereRadius);
                    break;
                case BoundingVolumeType.Region:
                    //Array order: west, south, east, north, minimum height, maximum height
                    double West = (boundingVolume.values[0] * 180.0f) / Mathf.PI;
                    double South = (boundingVolume.values[1] * 180.0f) / Mathf.PI;
                    double East = (boundingVolume.values[2] * 180.0f) / Mathf.PI;
                    double North = (boundingVolume.values[3] * 180.0f) / Mathf.PI;
                    double MaxHeight = boundingVolume.values[4];
                    double minHeight = boundingVolume.values[5];

                    var mincoord = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, South, West, minHeight).Convert(CoordinateSystem.WGS84_ECEF);
                    var maxcoord = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, South, West, minHeight).Convert(CoordinateSystem.WGS84_ECEF);

                    var coord = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, South, West, MaxHeight).Convert(CoordinateSystem.WGS84_ECEF);
                    if (coord.easting<mincoord.easting) mincoord.easting = coord.easting;
                    if (coord.northing<mincoord.northing) mincoord.northing = coord.northing;
                    if (coord.height < mincoord.height) mincoord.height = coord.height;
                    if (coord.easting >maxcoord.easting) maxcoord.easting = coord.easting;
                    if (coord.northing>maxcoord.northing) maxcoord.northing = coord.northing;
                    if (coord.height >maxcoord.height) maxcoord.height = coord.height;

                    coord = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, South, East, minHeight).Convert(CoordinateSystem.WGS84_ECEF);
                    if (coord.easting < mincoord.easting) mincoord.easting = coord.easting;
                    if (coord.northing < mincoord.northing) mincoord.northing = coord.northing;
                    if (coord.height < mincoord.height) mincoord.height = coord.height;
                    if (coord.easting > maxcoord.easting) maxcoord.easting = coord.easting;
                    if (coord.northing > maxcoord.northing) maxcoord.northing = coord.northing;
                    if (coord.height > maxcoord.height) maxcoord.height = coord.height;

                    coord = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, South, East, MaxHeight).Convert(CoordinateSystem.WGS84_ECEF);
                    if (coord.easting < mincoord.easting) mincoord.easting = coord.easting;
                    if (coord.northing < mincoord.northing) mincoord.northing = coord.northing;
                    if (coord.height < mincoord.height) mincoord.height = coord.height;
                    if (coord.easting > maxcoord.easting) maxcoord.easting = coord.easting;
                    if (coord.northing > maxcoord.northing) maxcoord.northing = coord.northing;
                    if (coord.height > maxcoord.height) maxcoord.height = coord.height;

                    coord = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, North, West, minHeight).Convert(CoordinateSystem.WGS84_ECEF);
                    if (coord.easting < mincoord.easting) mincoord.easting = coord.easting;
                    if (coord.northing < mincoord.northing) mincoord.northing = coord.northing;
                    if (coord.height < mincoord.height) mincoord.height = coord.height;
                    if (coord.easting > maxcoord.easting) maxcoord.easting = coord.easting;
                    if (coord.northing > maxcoord.northing) maxcoord.northing = coord.northing;
                    if (coord.height > maxcoord.height) maxcoord.height = coord.height;

                    coord = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, North, West, MaxHeight).Convert(CoordinateSystem.WGS84_ECEF);
                    if (coord.easting < mincoord.easting) mincoord.easting = coord.easting;
                    if (coord.northing < mincoord.northing) mincoord.northing = coord.northing;
                    if (coord.height < mincoord.height) mincoord.height = coord.height;
                    if (coord.easting > maxcoord.easting) maxcoord.easting = coord.easting;
                    if (coord.northing > maxcoord.northing) maxcoord.northing = coord.northing;
                    if (coord.height > maxcoord.height) maxcoord.height = coord.height;

                    coord = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, North, East, minHeight).Convert(CoordinateSystem.WGS84_ECEF);
                    if (coord.easting < mincoord.easting) mincoord.easting = coord.easting;
                    if (coord.northing < mincoord.northing) mincoord.northing = coord.northing;
                    if (coord.height < mincoord.height) mincoord.height = coord.height;
                    if (coord.easting > maxcoord.easting) maxcoord.easting = coord.easting;
                    if (coord.northing > maxcoord.northing) maxcoord.northing = coord.northing;
                    if (coord.height > maxcoord.height) maxcoord.height = coord.height;

                    coord = new Coordinate(CoordinateSystem.WGS84_LatLonHeight, North, East, MaxHeight).Convert(CoordinateSystem.WGS84_ECEF);
                    if (coord.easting < mincoord.easting) mincoord.easting = coord.easting;
                    if (coord.northing < mincoord.northing) mincoord.northing = coord.northing;
                    if (coord.height < mincoord.height) mincoord.height = coord.height;
                    if (coord.easting > maxcoord.easting) maxcoord.easting = coord.easting;
                    if (coord.northing > maxcoord.northing) maxcoord.northing = coord.northing;
                    if (coord.height > maxcoord.height) maxcoord.height = coord.height;

                    var unityMin = mincoord.ToUnity();
                    var unityMax = maxcoord.ToUnity();

                    unityBounds.size = Vector3.zero;
                    unityBounds.center = unityMin;
                    unityBounds.Encapsulate(unityMax);

                    BottomLeft = mincoord;
                    TopRight = maxcoord;
                    break;
                default:
                    break;
            }

            boundsAvailable = true;
        }

        public float getParentSSE()
        {
            float result = 0;
            if (parent!=null)
            {

            
            
            if (parent.content!=null)
            {
                if (parent.content.State==Content.ContentLoadState.DOWNLOADED)
                {
                    result = parent.screenSpaceError;
                }
            }
            if (result==0)
            {
                    result = parent.getParentSSE();
            }
            }
            return result;
        }

        public void Dispose()
        {
            if (content != null)
            {
                content.Dispose();
                content = null;
            }
        }
    
        public bool ChildrenHaveContent()
        {
            bool result = false;

            if (content!=null)
            { 
                result = true;
                return result;
            }
            foreach (Tile child in children)
            {
                if (child.ChildrenHaveContent()==true)
                { return true;}
            }
            return false;
        }
        public void DestroyChildTilesIfTilesetOutOfView(Camera ofCamera)
        {

            if (contentUri.Contains(".json") || contentUri.Contains(".subtree"))
            {

                if(IsInViewFrustrum(ofCamera)==false)
                {
                    DestroyChildTiles();
                }

            }
            else
            {
                foreach (Tile child in children)
                {
                    child.DestroyChildTilesIfTilesetOutOfView(ofCamera);
                }
            }

        }
        private void DestroyChildTiles()
        {
            for (int i = children.Count - 1; i >= 0 ; i--)
            {
                children[i].DestroyChildTiles();
                children[i] = null;
            }
        }
    }
}
