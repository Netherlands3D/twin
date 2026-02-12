using System;
using Netherlands3D.Tilekit.MemoryManagement;
using NUnit.Framework;
using Unity.Collections.LowLevel.Unsafe;

namespace Netherlands3D.Tilekit.Tests.MemoryManagement
{
    public class BlockMemoryArenaBlockRangeTests
    {
        [Test]
        [Category("Unit")]
        public void Ctor_StoresValues()
        {
            var r = new BlockMemoryArenaBlockRange(5, 3);
            Assert.That(r.Offset, Is.EqualTo(5));
            Assert.That(r.Count, Is.EqualTo(3));
        }

        [Test]
        [Category("Unit")]
        public void Ctor_Throws_OnNegativeOffset()
        {
            Assert.That(() => new BlockMemoryArenaBlockRange(-1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        [Category("Unit")]
        public void Ctor_Throws_OnNegativeCount()
        {
            Assert.That(() => new BlockMemoryArenaBlockRange(0, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        [Category("Unit")]
        public void Ctor_AllowsZeroOffsetAndCount()
        {
            var r = new BlockMemoryArenaBlockRange(0, 0);
            Assert.That(r.Offset, Is.EqualTo(0));
            Assert.That(r.Count, Is.EqualTo(0));
        }

        [Test]
        [Category("Unit")]
        public void Equals_EqualBasedOnValues()
        {
            var r1 = new BlockMemoryArenaBlockRange(5, 3);
            var r2 = new BlockMemoryArenaBlockRange(5, 3);
            var r3 = new BlockMemoryArenaBlockRange(3, 5);
            
            Assert.That(r1, Is.EqualTo(r2));
            Assert.That(r1, Is.Not.EqualTo(r3));
        }

        [Test]
        [Category("Unit")]
        public void Struct_ShouldHaveAFixedMemorySize()
        {
            // Not a regular test: but as a safeguard against multiplicative size increases we want to
            // very that this class is always this many bytes in size - this can also be used as living
            // documentation
            Assert.That(UnsafeUtility.SizeOf<BlockMemoryArenaBlockRange>(), Is.EqualTo(8));
        }
    }
}