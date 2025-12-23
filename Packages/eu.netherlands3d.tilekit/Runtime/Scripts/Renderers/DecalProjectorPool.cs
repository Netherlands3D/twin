using Netherlands3D.Functionalities.Wms;
using UnityEngine;
using UnityEngine.Pool;

namespace Netherlands3D.Tilekit.Renderers
{
    public class DecalProjectorPool
    {
        private readonly TextureDecalProjector textureDecalProjectorPrefab;
        private readonly GameObject parent;
        private readonly ObjectPool<TextureDecalProjector> projectorPool;

        public DecalProjectorPool(TextureDecalProjector textureDecalProjectorPrefab, GameObject parent)
        {
            this.textureDecalProjectorPrefab = textureDecalProjectorPrefab;
            this.parent = parent;

            projectorPool = new ObjectPool<TextureDecalProjector>(
                CreateProjectorForPool,
                actionOnGet: GetProjectorFromPool,
                actionOnRelease: ReleaseProjectorToPool
            );
        }

        private void GetProjectorFromPool(TextureDecalProjector projector)
        {
            projector.gameObject.SetActive(true);
        }

        private void ReleaseProjectorToPool(TextureDecalProjector projector)
        {
            projector.gameObject.SetActive(false);
            projector.SetTexture(null);
        }

        private TextureDecalProjector CreateProjectorForPool()
        {
            return Object.Instantiate(textureDecalProjectorPrefab, parent.transform);
        }

        public TextureDecalProjector Get()
        {
            return projectorPool.Get();
        }

        public void Release(TextureDecalProjector projector)
        {
            projectorPool.Release(projector);
        }
    }
}