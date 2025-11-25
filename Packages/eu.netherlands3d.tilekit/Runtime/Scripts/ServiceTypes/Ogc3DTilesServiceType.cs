using System;
using System.IO;
using KindMen.Uxios;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Tilekit.Archetypes;
using Netherlands3D.Tilekit.BoundingVolumes;
using Netherlands3D.Tilekit.TileBuilders;
using Unity.Mathematics;
using UnityEngine;

namespace Netherlands3D.Tilekit.ServiceTypes
{
    public class Ogc3DTilesServiceType : ServiceType<GenericArchetype, GenericArchetype.WarmTile, GenericArchetype.HotTile> 
    {
        public string Url;
        public Key Authorization;

        [Header("Area of Interest (RD)")]
        public int left = 153000;
        public int right = 158000;
        public int top = 462000;
        public int bottom = 467000;

        private BoxBoundingVolume AreaOfInterest =>
            BoxBoundingVolume.FromTopLeftAndBottomRight(
                new double3(left, top, 0),
                new double3(right, bottom, 0)
            );

        protected override GenericArchetype CreateArchetype() => new(AreaOfInterest);

        protected override void Initialize()
        {
            var promise = Uxios.DefaultInstance.Get<byte[]>(new Uri(Url));
            promise.Then(Build);
        }

        private void Build(IResponse obj)
        {
            using var stream = new MemoryStream(obj.Data as byte[] ?? Array.Empty<byte>());
            new Ogc3DTilesHydrator().Build(this.archetype.Cold, new() { Stream = stream });
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
    }
}