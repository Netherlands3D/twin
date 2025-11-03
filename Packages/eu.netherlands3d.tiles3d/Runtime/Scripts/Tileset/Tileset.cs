using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Netherlands3D.Tiles3D
{
    public enum ContentType
    {
        None,
        Geometry,
        Tileset,
        Subtree
    }

    public enum Version
    {
        unsupported,
        v1,
        v11
    }


    public abstract class childTile
    {
        public enum GeometryState
        {
            None,
            Loading,
            Loaded
        }

        public childTile parentNode;
        public List<childTile> children;
        public double[] transform;
        public BoundingVolume boundingVolume;
        public float geometricError;
        public TilingMethod tilingMethod = TilingMethod.Unknown;
        public ContentType contentType;
        public GeometryState geometryState = GeometryState.None;
        public string contentUri;
        public RefinementType refinementType;
    }
    public class ExplicitTile : childTile
    {
        public string ExternalTilesetUri;
        public ExplicitTile()
        {
            
        }
    }

    public class ImplicitTile : childTile
    {
        public string subtreeUri;
    }

    [System.Serializable]
    public class RootNode
    {
        Tileset parentTileset;
        public double[] transform;
        public BoundingVolume boundingVolume;
        public TilingMethod tilingMethod = TilingMethod.Unknown;
        float geometricError;
        RefinementType refinementType;

        // in case of implicit Tiling
        int availableLevels;
        SubdivisionScheme subdivisionScheme;
        int subtreeLevels;
        string subtreesUri;
        string contentUri;
        

        internal RootNode(JSONNode rootnodeJson, Tileset parentTileset)
        {
            this.parentTileset = parentTileset;
            transform = ReadTransform(rootnodeJson["transform"]);
            boundingVolume = ReadBoundingVolume(rootnodeJson["boundingVolume"]);
            geometricError = ReadGeometricError(rootnodeJson);
            tilingMethod = DetermineTilingMethod(rootnodeJson);

            switch (tilingMethod)
            {
                case TilingMethod.Unknown:
                    break;
                case TilingMethod.ExplicitTiling:

                    break;
                case TilingMethod.ImplicitTiling:
                    break;
                default:
                    break;
            }
        }

        private double[] ReadTransform(JSONNode transformNode)
        {
            transform = new double[16] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0 };
            if (transformNode != null)
            {
                for (int i = 0; i < 16; i++)
                {
                    transform[i] = transformNode[i].AsDouble;
                }
            }
            return transform;
        }
        private BoundingVolume ReadBoundingVolume(JSONNode boundingVolumeNode)
        {
            return ParseTileset.ParseBoundingVolume(boundingVolumeNode);
        }
        private TilingMethod DetermineTilingMethod(JSONNode rootnode)
        {
            if (rootnode["children"] != null)
            {
                return TilingMethod.ExplicitTiling;
            }
            else if (rootnode["implicitTiling"] != null)
            {
                return TilingMethod.ImplicitTiling;
            }
            return TilingMethod.Unknown;
        }
        private float ReadGeometricError(JSONNode rootnode)
        {
            return rootnode["geometricError"].AsFloat;
        }
    }
    [System.Serializable]
    public class Tileset
    {

        DownloadHelper downloadHelper;

        public string tilesetUrl;
        public Version version = Version.unsupported;

        public childTile rootnode;
        public TilingMethod tilingMethod{
        get{ return rootnode.tilingMethod; }        
    }
        JSONNode tilesetJSON;
        
        

        public Tileset(string tileseturl, DownloadHelper downloadHelper=null)
        {
            this.downloadHelper = downloadHelper;
            tilesetUrl = tileseturl;
            getDownloadHelper();
            downloadHelper.downloadData(tileseturl, receiveTileset);
        }
        private void getDownloadHelper()
        {
            if (downloadHelper == null)
            {
                downloadHelper = GameObject.FindAnyObjectByType<DownloadHelper>();
                if (downloadHelper == null)
                {
                    GameObject go = new GameObject("TilesetDownloadHelper");
                    downloadHelper = go.AddComponent<DownloadHelper>();
                }
                
            }
        }

        private void receiveTileset(DownloadHandler downloadHandler)
        {
            tilesetJSON = JSON.Parse(downloadHandler.text);

            ReadVersion(tilesetJSON["asset"]);

            //rootnode = new RootNode(tilesetJSON["root"], this);
        }

        private void ReadVersion(JSONNode assetnode)
        {
            string versiontext = assetnode["version"];
            if (versiontext == "1.0")
            {
                version = Version.v1;
            }
            else if (versiontext == "1.1")
            {
                version = Version.v11;
            }
            version = Version.unsupported;

        }
       
        
    }
}
