using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;

namespace Netherlands3D.Twin.Layers
{
    public class ATMLayerGameObject : LayerGameObject, ILayerWithPropertyData
    {
        public LayerPropertyData PropertyData => urlPropertyData;

        protected LayerURLPropertyData urlPropertyData = new();

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlProperty = (LayerURLPropertyData)properties.FirstOrDefault(p => p is LayerURLPropertyData);
            if (urlProperty != null)
            {
                this.urlPropertyData = urlProperty;
            }
        }
    }
}