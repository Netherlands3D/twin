#nullable enable

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;

namespace Netherlands3D.Tilekit.Profiling.Editor
{
    // Shows up as a Profiler module.
    [Serializable]
    [ProfilerModuleMetadata("Tilekit")] // Name shown in the Profiler module list
    public sealed class DataTelemetryProfilerModule : ProfilerModule
    {
        // These must match the counter names you publish from Telemetry.
        // (Counters are required for the module chart area to exist.)
        static readonly ProfilerCounterDescriptor[] k_ChartCounters =
        {
            new("DataSets", TilekitProfilerCategory.Category),
            new("Native Reserved (bytes)", TilekitProfilerCategory.Category),
            new("Native Used (bytes)", TilekitProfilerCategory.Category),
            new("Tiles Allocated", TilekitProfilerCategory.Category),
            new("Tiles Actual", TilekitProfilerCategory.Category),
            new("Warm Tiles", TilekitProfilerCategory.Category),
            new("Hot Tiles", TilekitProfilerCategory.Category),
        };

        // Auto-enable these categories when the module is selected.
        static readonly string[] k_AutoEnabledCategoryNames =
        {
            TilekitProfilerCategory.Category.Name
        };

        public DataTelemetryProfilerModule()
            : base(k_ChartCounters, autoEnabledCategoryNames: k_AutoEnabledCategoryNames)
        { }

        public override ProfilerModuleViewController CreateDetailsViewController()
            => new DataTelemetryDetailsViewController(ProfilerWindow);
    }
}