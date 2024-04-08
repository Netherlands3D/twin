using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using subtree;
using System.IO;

namespace Netherlands3D.Tiles3D
{
    public class ReadSubtree : MonoBehaviour
    {
        

        int currentSubtreeLevels;
        public Subtree subtree;
        public Tile tile;
        public ImplicitTilingSettings settings;
        public string subtreeUrl;
        System.Action<Tile> sendResult;
        private Tile appendTilesTo;

        public bool isbusy = false;

        public void DownloadSubtree(string url, ImplicitTilingSettings tilingSettings,Tile appendTilesTo, System.Action<Tile> callback)
        {
            isbusy = true;
            this.appendTilesTo = appendTilesTo;

            sendResult = callback;
            subtreeUrl = url;
            if (url == "")
            {
                Read3DTileset tilesetreader = GetComponent<Read3DTileset>();
                subtreeUrl = tilesetreader.tilesetUrl.Replace(tilesetreader.tilesetFilename, appendTilesTo.contentUri);
                                
            }
            Debug.Log($"loading subtree: {subtreeUrl}");
            //currentSubtreeLevels
            currentSubtreeLevels = settings.subtreeLevels;
            if (this.appendTilesTo.level + settings.subtreeLevels > settings.availableLevels)
            {
                currentSubtreeLevels = settings.availableLevels - this.appendTilesTo.level;
            }

            StartCoroutine(DownloadSubtree());
        }

        IEnumerator DownloadSubtree()
        {
            UnityWebRequest www = UnityWebRequest.Get(subtreeUrl);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
                Debug.Log(subtreeUrl);
                
            }
            else
            {
                byte[] subtreeData = www.downloadHandler.data;
                string tempFilePath = Path.Combine(Application.persistentDataPath, "st.subtree");
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
                File.WriteAllBytes(tempFilePath, subtreeData);
                using FileStream fileStream = File.Open(tempFilePath, FileMode.Open);
                using BinaryReader binaryReader = new(fileStream);

                subtree = SubtreeReader.ReadSubtree(binaryReader);
                
                // setup rootTile
                
                appendTilesTo.hascontent = (subtree.ContentAvailabiltyConstant == 1) || (subtree.ContentAvailability != null && subtree.ContentAvailability[0]);

                AddChildren(appendTilesTo, 0, 0);
                Read3DTileset tilesetreader = GetComponent<Read3DTileset>();
                //tilesetreader.root = tile;
                appendTilesTo.children.Add(tile);
                if (sendResult!=null)
                {
                    sendResult(appendTilesTo);
                }
                
            }
            isbusy = false;
            Debug.Log("subtree loaded");
        }

        public void AddChildren(Tile tile, int parentNortonIndex, int LevelStartIndex)
        {
            int localIndex = parentNortonIndex * 4;
            int levelstart = LevelStartIndex + (int)Mathf.Pow(4, (tile.level-appendTilesTo.level));
            
            AddChild(tile, localIndex, levelstart, 0);
            AddChild(tile, localIndex, levelstart, 1);
            AddChild(tile, localIndex, levelstart, 2);
            AddChild(tile, localIndex, levelstart, 3);
        }

        private void AddChild(Tile parentTile, int localIndex, int LevelStartIndex, int childNumber)
        {
            Tile childTile = new Tile();
            childTile.parent = parentTile;
            childTile.transform = parentTile.transform;
            childTile.level = parentTile.level + 1;
            childTile.X = parentTile.X * 2 + childNumber % 2;
            childTile.Y = parentTile.Y * 2;
            if (childNumber > 1)
            {
                childTile.Y += 1;
            }
            childTile.geometricError = parentTile.geometricError / 2f;
            childTile.boundingVolume = parentTile.boundingVolume.GetChildBoundingVolume(childNumber,settings.subdivisionScheme);
           
            // check geometric content
            if (childTile.level < appendTilesTo.level + currentSubtreeLevels)
            {
                childTile.hascontent = (subtree.ContentAvailabiltyConstant == 1) || (subtree.ContentAvailability != null && subtree.ContentAvailability[localIndex + LevelStartIndex + childNumber]);
                if (childTile.hascontent)
                {
                    childTile.contentUri = (settings.contentUri.Replace("{level}", childTile.level.ToString()).Replace("{x}", childTile.X.ToString()).Replace("{y}", childTile.Y.ToString())); ;
                }

            }

            // check childTileAvailability
            if (childTile.level<appendTilesTo.level+currentSubtreeLevels)
            {
                if (subtree.TileAvailabiltyConstant == 1 || subtree.TileAvailability[localIndex + LevelStartIndex + childNumber])
                {

                    AddChildren(childTile, localIndex + childNumber, LevelStartIndex);
                }
            }

            //check subtree-availability
            if (childTile.level == appendTilesTo.level + currentSubtreeLevels)
            {

            int indexnumber = localIndex + childNumber;
            bool hasSubtree = true;
            if (subtree.ChildSubtreeAvailability == null)
            {
                hasSubtree = false;
            }
            else
            {
                hasSubtree = subtree.ChildSubtreeAvailability[indexnumber];
            }
            if (hasSubtree)
            {

                childTile.hascontent = true;
                childTile.contentUri = (settings.subtreeUri.Replace("{level}", childTile.level.ToString()).Replace("{x}", childTile.X.ToString()).Replace("{y}", childTile.Y.ToString())); ;
                Debug.Log("child has subtree");

            }

        }

            if (childTile.hascontent || childTile.children.Count>0)
            {
                parentTile.children.Add(childTile);
            }
            
        }
    }
}
