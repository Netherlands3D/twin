using System.Threading.Tasks;
using Netherlands3D.Twin.Layers.LayerTypes;

namespace Netherlands3D.Twin.Services
{
    public interface ILayersServiceFacade
    {
        public Task<ReferencedLayerData> Add(ILayerBuilder layerBuilder);
    }
}