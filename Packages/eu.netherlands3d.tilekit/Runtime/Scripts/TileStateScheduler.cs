using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Netherlands3D.Tilekit.WriteModel;

namespace Netherlands3D.Tilekit
{
    public class TileStateScheduler
    {
        private readonly TilesSelector tileSelector;
        private readonly ITileLifecycleBehaviour tileLifecycleBehaviour;
        private readonly TileSet tileSet;

        private readonly Plane[] frustumPlanes = new Plane[6];
        private readonly float warmFeather;

        public TileStateScheduler(
            TilesSelector tileSelector,
            ITileLifecycleBehaviour tileLifecycleBehaviour,
            TileSet tileSet,
            float warmFeather = 1.25f
        )
        {
            this.tileSelector = tileSelector;
            this.tileLifecycleBehaviour = tileLifecycleBehaviour;
            this.tileSet = tileSet;
            this.warmFeather = Mathf.Max(1f, warmFeather);
        }

        public void Schedule()
        {
            var camera = Camera.main;
            if (camera == null) return;

            GeometryUtility.CalculateFrustumPlanes(camera, frustumPlanes);

            int warmCap = Math.Max(16, tileSet.Warm.Capacity);
            int hotCap = Math.Max(8, tileSet.Hot.Capacity);

            var desiredHot = new NativeHashSet<int>(hotCap, Allocator.Temp);
            var desiredWarm = new NativeHashSet<int>(warmCap, Allocator.Temp);

            var currentWarm = new NativeHashSet<int>(warmCap, Allocator.Temp);
            var currentHot = new NativeHashSet<int>(hotCap, Allocator.Temp);

            var shouldWarmUp = new NativeHashSet<int>(warmCap, Allocator.Temp);
            var shouldFreeze = new NativeHashSet<int>(warmCap, Allocator.Temp);
            var shouldHeatUp = new NativeHashSet<int>(hotCap, Allocator.Temp);
            var shouldCooldown = new NativeHashSet<int>(hotCap, Allocator.Temp);

            // 1) Select desired states
            tileSelector.Select(desiredHot, tileSet.Root, frustumPlanes, 1f);
            tileSelector.Select(desiredWarm, tileSet.Root, frustumPlanes, warmFeather);

            // Ensure hot is always a subset of warm
            foreach (var h in desiredHot)
                desiredWarm.Add(h);

            // 2) Snapshot current states
            for (int i = 0; i < tileSet.Warm.Length; i++)
                currentWarm.Add(tileSet.Warm[i]);

            for (int i = 0; i < tileSet.Hot.Length; i++)
                currentHot.Add(tileSet.Hot[i]);

            // 3) Warm diffs
            foreach (var w in desiredWarm)
                if (!currentWarm.Contains(w))
                    shouldWarmUp.Add(w);

            foreach (var w in currentWarm)
                if (!desiredWarm.Contains(w))
                    shouldFreeze.Add(w);

            // 4) Hot diffs
            foreach (var h in desiredHot)
                if (!currentHot.Contains(h))
                    shouldHeatUp.Add(h);

            foreach (var h in currentHot)
                if (!desiredHot.Contains(h))
                    shouldCooldown.Add(h);

            // 5) Dispatch in a sensible order
            InvokeWarmUp(shouldWarmUp);
            InvokeHeatUp(shouldHeatUp);
            InvokeCooldown(shouldCooldown);
            InvokeFreeze(shouldFreeze);

            desiredHot.Dispose();
            desiredWarm.Dispose();
            currentWarm.Dispose();
            currentHot.Dispose();
            shouldWarmUp.Dispose();
            shouldHeatUp.Dispose();
            shouldCooldown.Dispose();
            shouldFreeze.Dispose();
        }

        private void InvokeWarmUp(NativeHashSet<int> set)
        {
            if (set.Count == 0) return;
            var arr = set.ToNativeArray(Allocator.Temp);
            tileLifecycleBehaviour.OnWarmUp(ToReadOnlySpan(arr));
            arr.Dispose();
        }

        private void InvokeHeatUp(NativeHashSet<int> set)
        {
            if (set.Count == 0) return;
            var arr = set.ToNativeArray(Allocator.Temp);
            tileLifecycleBehaviour.OnHeatUp(ToReadOnlySpan(arr));
            arr.Dispose();
        }

        private void InvokeCooldown(NativeHashSet<int> set)
        {
            if (set.Count == 0) return;
            var arr = set.ToNativeArray(Allocator.Temp);
            tileLifecycleBehaviour.OnCooldown(ToReadOnlySpan(arr));
            arr.Dispose();
        }

        private void InvokeFreeze(NativeHashSet<int> set)
        {
            if (set.Count == 0) return;
            var arr = set.ToNativeArray(Allocator.Temp);
            tileLifecycleBehaviour.OnFreeze(ToReadOnlySpan(arr));
            arr.Dispose();
        }

        private static ReadOnlySpan<int> ToReadOnlySpan(NativeArray<int> arr)
        {
            return arr.AsReadOnlySpan();
        }
    }
}