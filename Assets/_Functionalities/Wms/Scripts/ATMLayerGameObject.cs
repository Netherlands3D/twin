using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Rendering;
using Netherlands3D.Twin._Functionalities.Wms.Scripts;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    public class ATMLayerGameObject : CartesianTileLayerGameObject, ILayerWithPropertyData
    {
        public LayerPropertyData PropertyData => urlPropertyData;

        protected LayerURLPropertyData urlPropertyData = new();

        private ATMLayerManager layerManager;

        void Update()
        {
            layerManager?.SwitchLayerToCurrentZoomLevel();
        }
        
        protected override void Start()
        {
            base.Start();
            LayerData.LayerOrderChanged.AddListener(SetRenderOrder);

            ATMLayerGameObject parent = transform.parent.GetComponent<ATMLayerGameObject>();
            if (parent == null) DoStartAsParent();
        }

        private void DoStartAsParent()
        {
            var currentDataLayer = GetComponent<ATMTileDataLayer>();
            TextureProjectorBase projectorPrefab = currentDataLayer.ProjectorPrefab;
            currentDataLayer.SetZoomLevel(-1);
            currentDataLayer.isEnabled = false;

            Destroy(GetComponent<WorldTransform>());
            Destroy(GetComponent<ChildWorldTransformShifter>());

            SetRenderOrder(LayerData.RootIndex);

            layerManager = new ATMLayerManager(gameObject.AddComponent<ATMDataController>());
            layerManager.CreateTileHandlerForEachZoomLevel(transform, projectorPrefab);
            layerManager.SwitchLayerToCurrentZoomLevel(true);
        }


        private void SetRenderOrder(int order)
        {
            if (layerManager == null) return;

            //we have to flip the value because a lower layer with a higher index needs a lower render index
            layerManager.ATMProjectionLayer.RenderIndex = -order;
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
            layerManager?.Dispose();
        }
    }
}