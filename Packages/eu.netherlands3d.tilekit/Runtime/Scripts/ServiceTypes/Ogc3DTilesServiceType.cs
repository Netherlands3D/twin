using System;
using System.IO;
using KindMen.Uxios;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Tilekit.Archetypes;
using Netherlands3D.Tilekit.BoundingVolumes;
using Netherlands3D.Tilekit.TileBuilders;
using Netherlands3D.Tilekit.WriteModel;
using Unity.Mathematics;

namespace Netherlands3D.Tilekit.ServiceTypes
{
    // TODO: This is not a raster archetype, but we fake it for now
    public class Ogc3DTilesServiceType : ServiceType<RasterArchetype, RasterArchetype.WarmTile, RasterArchetype.HotTile>
    {
        public string Url;
        public Key Authorization;
        private ColdStorage coldStorage;

        protected override void Initialize()
        {
            var promise = Uxios.DefaultInstance.Get<byte[]>(new Uri(Url));
            promise.Then(Build);
        }

        private void Build(IResponse obj)
        {
            using var stream = new MemoryStream(obj.Data as byte[] ?? Array.Empty<byte>());
            coldStorage = new ColdStorage(AreaOfInterest, 1024);
            new Ogc3DTilesHydrator().Build(coldStorage, new() {Stream = stream});
        }

        protected override void OnTick()
        {
            // Let's skip this while prototyping
        }

        public override void OnWarmUp(ReadOnlySpan<int> candidateTileIndices)
        {
            
        }

        public override void OnHeatUp(ReadOnlySpan<int> candidateTileIndices)
        {
           
        }

        public override void OnCooldown(ReadOnlySpan<int> candidateTileIndices)
        {
            
        }

        public override void OnFreeze(ReadOnlySpan<int> candidateTileIndices)
        {
            
        }

        protected override void OnDestroy()
        {
            coldStorage?.Dispose();
            base.OnDestroy();
        }

        private static BoxBoundingVolume AreaOfInterest
        {
            get
            {
                int left = 153000;
                int right = 158000;
                int top = 462000;
                int bottom = 467000;

                var areaOfInterest = BoxBoundingVolume.FromTopLeftAndBottomRight(
                    new double3(left, top, 0),
                    new double3(right, bottom, 0)
                );
                return areaOfInterest;
            }
        }
    }
}