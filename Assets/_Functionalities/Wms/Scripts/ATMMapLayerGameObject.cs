namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    public class ATMMapLayerGameObject : CartesianTileLayerGameObject
    {

        protected override void Start()
        {
            base.Start();

            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);
        }

        private void SetRenderOrder(int order)
        {
            // if (layerManager == null) return;

            //we have to flip the value because a lower layer with a higher index needs a lower render index
            // layerManager.ATMProjectionLayer.RenderIndex = -order;
        }

        public override void DestroyLayerGameObject()
        {
            base.DestroyLayerGameObject();
            LayerData.LayerOrderChanged.RemoveListener(SetRenderOrder);
        }
    }
}