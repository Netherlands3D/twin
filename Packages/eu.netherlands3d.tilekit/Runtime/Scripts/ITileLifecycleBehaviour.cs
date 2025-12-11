using System;

namespace Netherlands3D.Tilekit
{
    public interface ITileLifecycleBehaviour
    {
        public void OnWarmUp(ReadOnlySpan<int> candidateTileIndices);
        public void OnHeatUp(ReadOnlySpan<int> candidateTileIndices);
        public void OnCooldown(ReadOnlySpan<int> candidateTileIndices);
        public void OnFreeze(ReadOnlySpan<int> candidateTileIndices);
    }
}