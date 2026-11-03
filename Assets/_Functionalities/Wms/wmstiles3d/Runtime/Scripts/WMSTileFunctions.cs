
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using System;
using Unity.Mathematics;
using PlasticGui.Help.Conditions;
using Netherlands3D.Functionalities.Wms;

namespace Netherlands3D.WMSTiles3D
{
    [System.Serializable]
    public struct wmsServerProperties
    {
        public string name;
        public string version;
        public string spatialReferenceType;
        public CoordinateSystem coordinateSystem;
        public string style;
        public int maxWidth;
        public int maxHeight;
        public bool transparent;
        public long maxScaleDenominator;
        public BoundingBox boundingBox;
        private int _maximumZoomLevel;
        public int maximumZoomlevel{ 
            get{
                if(_maximumZoomLevel ==0)
                {
                    double maximumScreenspaceError = maxScaleDenominator*0.00028*maxWidth;
                    double maxZoom = maximumScreenspaceError/MinimumScreenSpaceError;
                    double maxzoomlevel = math.log2(maxZoom);
                    _maximumZoomLevel= (int)math.floor(maxzoomlevel);
                }
                return _maximumZoomLevel;
                }
        }
        public float MinimumScreenSpaceError;
        public int MaxTreeDepth;
        public string baseUrl;

        public string Format;

        public string getMapRequestUrl(Coordinate BottomLeft,Coordinate Topright)
        {
            string result  = $"{baseUrl}?service=WMS&request=GetMap&version={version}&srs=EPSG:28992&crs=EPSG:28992&Style={style}&Layers={name}&Width={maxWidth}&Height={maxHeight}&format={Format}&BBox={BottomLeft.easting},{BottomLeft.northing},{Topright.easting},{Topright.northing}";
            return result;
        }
    }
    public class WMSTileFunctions : TileFunctions
    {

        //for testing only
        public string tilesetUrl;
        private Tileset3DHandler tilesetHandler;
        internal HeightMap heightmap = null;
        
        public wmsServerProperties wmsServerProperties=new wmsServerProperties();
        
        public MapFilters mapfilter;
        public GameObject projectorPrefab;

        public TilePrioritiser tilePrioritiser;
        public void Initialize()
        {
            
            setupLufo();
            tilesetHandler = new Tileset3DHandler();
            tilesetHandler.contentCoordinateSystem = CoordinateSystem.RDNAP;
            tilesetHandler.currentCamera = UnityEngine.Camera.current;
            tilesetHandler.tilePrioritiser = tilePrioritiser;
            tilesetHandler.ContentCreator = CreateContent;
            tilesetHandler.SubtreeCreator = AddSubtree;
            tilesetHandler.rootnode = new Tile();
            createRootNode(ref tilesetHandler.rootnode);

        }
        

        public void createRootNode(ref Tile rootnode)
        {
            if(heightmap == null)
            {
                heightmap = FindFirstObjectByType<HeightMap>();
            }
           Tile root = new Tile();
           root.tileSet = tilesetHandler;
           root.boundingVolume = new BoundingVolume();
            root.boundingVolume.boundingVolumeType = BoundingVolumeType.Box;
            root.refine="REPLACE";
            Coordinate center = wmsServerProperties.boundingBox.Center;
            double boxWidth = wmsServerProperties.boundingBox.TopRight.easting - wmsServerProperties.boundingBox.BottomLeft.easting;
            double boxHeight = wmsServerProperties.boundingBox.TopRight.northing - wmsServerProperties.boundingBox.BottomLeft.northing;
            double tileSize = math.max(boxWidth,boxHeight);

            double centerElevation = heightmap.GetHeight(center);
            center.height=centerElevation;
            double elevationMax = centerElevation+10;
            double elevationMin = centerElevation-10;
            double averageElevation = (elevationMax + elevationMin)/2;
            double halfElevation = elevationMax - averageElevation;

            root.boundingVolume.values = new double[12]{center.easting,center.northing,averageElevation,tileSize/2,0,0,0,tileSize/2,0,0,0,halfElevation};

            double[] transformValues = new double[16] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 };
            root.tileTransform = new TileTransform(transformValues);
            root.level = 0;
            root.X=0;
            root.Y=0;
            root.contentType = TileContentType.WMS;
            root.hascontent=true;
            root.geometricError = tileSize / wmsServerProperties.maxWidth;
            double maximumBoxWidth = wmsServerProperties.maxScaleDenominator*0.00028d*wmsServerProperties.maxWidth;
            if (maximumBoxWidth<tileSize)
            {
                root.contentType = TileContentType.None;
                root.hascontent=false;
            }
            root.children.Add(CreateChildTile(root,QuadtreeQuadrant.Topleft));
            root.children.Add(CreateChildTile(root,QuadtreeQuadrant.Topright));
            root.children.Add(CreateChildTile(root,QuadtreeQuadrant.BottomLeft));
            root.children.Add(CreateChildTile(root,QuadtreeQuadrant.BottomRight));
            
            rootnode = root;
        }
        internal Tile CreateChildTile(Tile parentTile, QuadtreeQuadrant quadtreeQuadrant)
        {
            Tile newTile = new Tile();
            newTile.tileSet = parentTile.tileSet;
            newTile.parent = parentTile;
            newTile.level = parentTile.level+1;
            newTile.tileSet = parentTile.tileSet;
            newTile.X = 2*parentTile.X + ((int)quadtreeQuadrant%2);
            newTile.Y = 2*parentTile.Y + (int)math.floor((int)quadtreeQuadrant/2);
            newTile.refine = "REPLACE";
            double boxWidth = parentTile.boundingVolume.values[3];
            newTile.contentType = parentTile.contentType;
            newTile.hascontent = parentTile.hascontent;
            if(parentTile.hascontent==false)
            {
                double maximumBoxWidth = wmsServerProperties.maxScaleDenominator*0.00028d*wmsServerProperties.maxWidth;
                if (maximumBoxWidth>boxWidth)
                {
                    newTile.contentType = TileContentType.WMS;
                    newTile.hascontent=true;
                }
            }
            newTile.geometricError = parentTile.geometricError*0.5;
            BoundingVolume newBoundingVolume = parentTile.boundingVolume.GetChildBoundingVolume((int)quadtreeQuadrant,SubdivisionScheme.Quadtree);
            Coordinate center = new Coordinate(CoordinateSystem.RD,newBoundingVolume.values[0],newBoundingVolume.values[1]);
            float centerelevation = heightmap.GetHeight(center);
            newBoundingVolume.values[2]=centerelevation;
            newTile.boundingVolume = newBoundingVolume;
            if (wmsServerProperties.maximumZoomlevel==newTile.level) return newTile;

            if(newTile.level%wmsServerProperties.MaxTreeDepth==0)
                {
                newTile.hasSubtree=true;
                return newTile;
                }
            newTile.children.Add(CreateChildTile(newTile,QuadtreeQuadrant.Topleft));
            newTile.children.Add(CreateChildTile(newTile,QuadtreeQuadrant.Topright));
            newTile.children.Add(CreateChildTile(newTile,QuadtreeQuadrant.BottomLeft));
            newTile.children.Add(CreateChildTile(newTile,QuadtreeQuadrant.BottomRight));

           return newTile; 
        }
        public override void AddSubtree(Tile parentTile)
        {
            parentTile.children.Add(CreateChildTile(parentTile,QuadtreeQuadrant.Topleft));
            parentTile.children.Add(CreateChildTile(parentTile,QuadtreeQuadrant.Topright));
            parentTile.children.Add(CreateChildTile(parentTile,QuadtreeQuadrant.BottomLeft));
            parentTile.children.Add(CreateChildTile(parentTile,QuadtreeQuadrant.BottomRight));
        }

        public override void CreateContent(Tile parentTile)
        {
            GameObject contentContainer = new GameObject();
            contentContainer.transform.parent = transform;
            WMSContent content = contentContainer.AddComponent<WMSContent>();
            
            content.projectorPrefab = projectorPrefab;
            content.getMapRequestUrl = GetContentUrl;
            content.parentTile = parentTile;
            parentTile.content = content;
        }
        public string GetContentUrl(Tile parentTile)
        {
            var boundingBox = new BoundingBox(parentTile.BottomLeft,parentTile.TopRight);
            string url = mapfilter.ToUrlBasedOn(new Uri(tilesetUrl)).ToString().Replace("{0}", boundingBox.ToString());
            Debug.Log(url);
             return url;
        }

        private void setupLufo()
        {
            wmsServerProperties = new wmsServerProperties();
            Coordinate BottomLeft = new Coordinate(CoordinateSystem.RD,-2000.0,290000.0);
            Coordinate Topright = new Coordinate(CoordinateSystem.RD, 294000.0,630000.0);
            wmsServerProperties.boundingBox = new BoundingBox(BottomLeft,Topright);
            wmsServerProperties.coordinateSystem = CoordinateSystem.RD;
            wmsServerProperties.maxHeight = 2500;
            wmsServerProperties.maxWidth = 2500;
            wmsServerProperties.maxScaleDenominator = 150000;
            wmsServerProperties.name = "Actueel_orthoHR";
            wmsServerProperties.style = "default";
            wmsServerProperties.version = "1.3";
            wmsServerProperties.transparent = true;
            wmsServerProperties.MinimumScreenSpaceError=2f;
            wmsServerProperties.MaxTreeDepth=3;
            wmsServerProperties.baseUrl = "https://service.pdok.nl/hwh/luchtfotorgb/wms/v1_0";
            wmsServerProperties.Format = "image/jpg";
        }

        public void Update()
        {
            if (tilesetHandler==null)return;
            if(mapfilter.name.Length==0)return;
            tilesetHandler.LoadInView();
        }

        public void ReloadTileContent()
        {
            tilesetHandler.DestroyAllVisibleTiles();
        }
    }
}
