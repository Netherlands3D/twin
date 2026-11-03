using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using KindMen.Uxios;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Tiles3D;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

namespace Netherlands3D.WMSTiles3D
{
    public class WMSContent : Content
    {
        public GameObject projectorPrefab;

        
        public Func<Tile, string> getMapRequestUrl;
        public override void Load()
        {
            
            if (State == ContentLoadState.DOWNLOADING || State == ContentLoadState.DOWNLOADED)
                return;
            State = ContentLoadState.DOWNLOADING;
            
            parentTile.isLoading = true;
            Debug.Log("start loading");
            StartCoroutine(startloading()) ;
        }

        IEnumerator startloading()
        {
            //Debug.Log("loading started");
            yield return null;
            string wmsUrl = getMapRequestUrl(parentTile);
            
            //Debug.Log(wmsUrl);
            //on loading project form save file this can be empty 
           if (string.IsNullOrEmpty(wmsUrl)) yield break;
            var promise = Uxios.DefaultInstance.Get<Texture2D>(new Uri(wmsUrl));
            promise.Then(OnDownloadedTexture);
            promise.Catch(OnFailedToDownloadTexture);
            yield return Uxios.WaitForRequest(promise);

        }

        private void OnDownloadedTexture(IResponse response)
        {
           // var payload = response.Config.GetPayload<WMSTileDataLayerChangePayload>();
           // var tileKey = payload.TileKey;
           var tex = response.Data as Texture2D;

            if (!tex)
            {
                FinishedLoading(false);  
                //onLogMessage.Invoke(LogType.Warning, $"Texture could not load for tile '{tileKey.x},{tileKey.y}'");
                return;
            }
            GameObject projectorObject = Instantiate(projectorPrefab);
            
            projectorObject.transform.parent = this.transform;
           TextureDecalProjector projector = projectorObject.GetComponent<TextureDecalProjector>();
            projector.Project(tex, (float)(parentTile.TopRight.easting-parentTile.BottomLeft.easting),100,-1,110,true);

             WorldTransform worldtransform = projectorObject.AddComponent<WorldTransform>();
            worldtransform.MoveToCoordinate(new Coordinate(CoordinateSystem.RDNAP,parentTile.boundingVolume.values[0],parentTile.boundingVolume.values[1],parentTile.boundingVolume.values[2]));
                  FinishedLoading(true);  
        }

        private void OnFailedToDownloadTexture(Exception exception)
        {
 
        }

        private void FinishedLoading(bool succes)
        {
            parentTile.isLoading=false;
            State = ContentLoadState.DOWNLOADED;
            onDoneDownloading.Invoke();
            onTileLoadCompleted.Invoke(this);
        }

 public override void Dispose()
        {
            Destroy(this.gameObject);            
        }
    }
}
