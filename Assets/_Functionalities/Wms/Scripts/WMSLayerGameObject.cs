using System;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Legend;
using Netherlands3D.WMSTiles3D;

namespace Netherlands3D.Functionalities.Wms
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    [RequireComponent(typeof(WMSTileDataLayer))]

    [RequireComponent(typeof(ICredentialHandler))]
    public class WMSLayerGameObject : CartesianTileLayerGameObject, IVisualizationWithPropertyData
    {
        private WMSTileDataLayer wmsProjectionLayer;

        private ICredentialHandler credentialHandler;
        
        
        public bool TransparencyEnabled = true; //this gives the requesting url the extra param to set transparancy enabled by default       
        public int DefaultEnabledLayersMax = 5; //in case the dataset is very large with many layers. lets topggle the layers after this count to not visible.
        public Vector2Int PreferredImageSize = Vector2Int.one * 512;
        public bool ShowLegendOnSelect { get; set; } = true;
        public override BoundingBox Bounds => wmsProjectionLayer?.BoundingBox;
        
        //The following is needed to send a valid getMap request to check for credentials. We don't want to waste many resources, so request a texture of 1x1 px
        public const string testBBox = "0,300000,280000,625000";
        private static readonly Regex WidthRegex = new(@"([?&]width=)\d+", RegexOptions.IgnoreCase);
        private static readonly Regex HeightRegex = new(@"([?&]height=)\d+", RegexOptions.IgnoreCase);
        
        protected override void OnVisualizationInitialize()
        {
           
        }

        protected override void OnVisualizationReady()
        {
           
        }

        public void SetLegendActive(bool active)
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            if (urlPropertyData.Url == null) return;

            if (!LayerData.HasValidCredentials)
                active = false;
            
            ServiceLocator.GetService<LegendBehaviour>().ShowLegend(urlPropertyData.Url.ToString(), active);
        }

        //a higher order means rendering over lower indices
        private void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
           
        }

        private void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            
          
        }

        public void ClearCredentials()
        {
            
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            
            
        }

        private void UpdateURL(Uri storedUri)
        {
           
        }

        protected override void RegisterEventListeners()
        {
            
        }

        protected override void UnregisterEventListeners()
        {
            
        }

        public override void OnSelect(LayerData layer)
        {
           
        }

        public override void OnDeselect(LayerData layer)
        {
           
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
          
        }

        private void SetBoundingBox(BoundingBoxContainer boundingBoxContainer)
        {
           
        }
    }
}