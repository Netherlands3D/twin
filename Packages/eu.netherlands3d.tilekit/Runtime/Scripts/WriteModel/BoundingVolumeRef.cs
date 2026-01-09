using System.Runtime.InteropServices;

namespace Netherlands3D.Tilekit.WriteModel
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BoundingVolumeRef // 1 byte (+ 3 B padding) + 1 int = 8 B
    {
        public readonly BoundingVolumeType Type; // 1 B (will pad to 8 on 64-bit; still tiny)
        public readonly int Index; // index into the corresponding pool

        public BoundingVolumeRef(BoundingVolumeType type, int index)
        {
            Type = type;
            Index = index;
        }
    }
}