using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    public class ATMLayerGameObject : CartesianTileLayerGameObject, ILayerWithPropertyData
    {
        public ATMTileDataLayer WMSProjectionLayer => atmProjectionLayer;
        public bool TransparencyEnabled = true; //this gives the requesting url the extra param to set transparancy enabled by default       
        public int DefaultEnabledLayersMax = 5;  //in case the dataset is very large with many layers. lets topggle the layers after this count to not visible.
        public Vector2Int PreferredImageSize = Vector2Int.one * 512;
        public LayerPropertyData PropertyData => urlPropertyData;

        private ATMTileDataLayer atmProjectionLayer;
        protected LayerURLPropertyData urlPropertyData = new();

        protected override void Awake()
        {
            base.Awake();
            atmProjectionLayer = GetComponent<ATMTileDataLayer>();
        }

        protected override void Start()
        {
            base.Start();
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);
            SetRenderOrder(LayerData.RootIndex);
        }

        //a higher order means rendering over lower indices
        public void SetRenderOrder(int order)
        {
            //we have to flip the value because a lower layer with a higher index needs a lower render index
            WMSProjectionLayer.RenderIndex = -order;
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
            }
        }

        public override void DestroyLayerGameObject()
        {
            base.DestroyLayerGameObject();
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
        }
    }
}