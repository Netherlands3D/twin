using System;
using System.Collections.Generic;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Services;
using Netherlands3D.Twin.Services.Netherlands3D;
using Netherlands3D.Twin.Tools;
using UnityEngine;
using UnityEngine.Profiling;

namespace Netherlands3D.Twin.Services
{
    public class DebugStatsService : MonoBehaviour
    {

        public sealed class StatSampler
        {
            public DebugStat Stat { get; }
            public float Interval { get; }
            public Func<double> ReadValue { get; }
            public float ElapsedTime { get; set; }

            public StatSampler(DebugStat stat, float interval, Func<double> readValue)
            {
                Stat = stat;
                Interval = interval;
                ReadValue = readValue;
            }
            
        }

        private const float BytesToMegabytes = 1f / (1024f * 1024f);
        
        private readonly List<DebugStat> stats = new();
        private readonly List<StatSampler> samplers = new();
        
        public IReadOnlyList<DebugStat> Stats => stats;

        

        private TileHandler TileHandler => ServiceLocator.GetService<TileHandler>();

        [SerializeField] private Tool debugStatsTool;
        
        void Awake()
        {

            var frameDurationCategory = new DebugStatCategory("Frame duration");
            
            AddStat("Frame duration (ms)", frameDurationCategory, 0, 
                () => Time.unscaledDeltaTime * 1000d);

            {
                var previousFrameCount = Time.frameCount;
                var interval = .25f;
                AddStat("Frame rate (frames/second)", frameDurationCategory, interval,
                    () =>
                    {
                        var returnValue = (Time.frameCount - previousFrameCount) / interval;
                        previousFrameCount = Time.frameCount;
                        return returnValue;
                    });
            }

            //
            
            var memoryCategory = new DebugStatCategory("Memory");
            
#if UNITY_WEBGL && !UNITY_EDITOR
            AddStat("WebAssembly heap size (MB)", memoryCategory, 1f,
                () => SystemInfo.systemMemorySize);
#endif
            
            AddStat("Total used memory (MB)", memoryCategory, .25f, 
                () => Profiler.GetTotalAllocatedMemoryLong() * BytesToMegabytes);
            
            AddStat("Total reserved memory (MB)", memoryCategory, .25f, 
                () => Profiler.GetTotalReservedMemoryLong() * BytesToMegabytes);

            AddStat("GC Used Memory (MB)", memoryCategory, .25f,
                () => Profiler.GetMonoUsedSizeLong() * BytesToMegabytes);
            
            AddStat("GC Reserved Memory (MB)", memoryCategory, .25f,
                () => Profiler.GetMonoHeapSizeLong() * BytesToMegabytes);
            
            //
            
            var cartesianTilesCategory = new DebugStatCategory("Cartesian tiles");
            
            AddStat("Loaded cartesian tile count", cartesianTilesCategory, .25f, 
                () => TileHandler.LoadedTileCount);
            
            AddStat("Pending cartesian tile change count", cartesianTilesCategory, .25f, 
                () => TileHandler.PendingTileChangeCount);
            
            AddStat("Active cartesian tile count", cartesianTilesCategory, .25f, 
                () => TileHandler.ActiveTileChangeCount);
        }

        void Update()
        {
            if (!debugStatsTool.Available)
                return;
            
            var deltaTime = Time.deltaTime;
            foreach (var sampler in samplers)
            {
                if (sampler.Interval <= 0)
                {
                    Sample(sampler);
                    continue;
                }

                sampler.ElapsedTime += deltaTime;

                if (sampler.ElapsedTime < sampler.Interval)
                {
                    continue;
                }
                
                sampler.ElapsedTime %= sampler.Interval;
                Sample(sampler);
            }

        }


        private void AddStat(string displayName, DebugStatCategory category, float interval, Func<double> readValue)
        {
            var stat = new DebugStat(displayName, category);
            
            stats.Add(stat);
            samplers.Add(new StatSampler(stat, interval, readValue));
        }
        
        private void Sample(StatSampler sampler) 
        {
            sampler.Stat.AddValue(sampler.ReadValue());
        }

    }

}
