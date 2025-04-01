using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin.Layers
{
    public class ManagedMaterial
    {
        private readonly Func<Material> materialCreator;
        private readonly Func<Material> materialGetter;
        private readonly Action<Material> materialSetter;

        public ManagedMaterial(
            Func<Material> materialCreator,
            Func<Material> materialGetter,
            Action<Material> materialSetter
        ) {
            this.materialCreator = materialCreator;
            this.materialGetter = materialGetter;
            this.materialSetter = materialSetter;
        }

        /// <summary>
        /// Replaces the material in a way as to prevent mem leaks.
        /// </summary>
        public void UpdateMaterial()
        {
            // Get a cached version of the old and new material to prevent issues with creators and setters
            // overwriting the retrieved materials
            var newMaterial = materialCreator();
            var oldMaterial = materialGetter();

            // only replace when needed
            if (oldMaterial != newMaterial)
            {
                // Clean up after ourselves - prevents memleaks by releasing C++ materials
                Object.Destroy(oldMaterial);

                // Set explicitly to null to prevent possible C# Managed Shell leaks
                materialSetter(null);

                // Apply a newly created material
                materialSetter(newMaterial);
            }

            // Set explicitly to null to prevent possible C# Managed Shell leaks
            newMaterial = null;
            oldMaterial = null;
        }

        // Implicit cast to Material
        public static implicit operator Material(ManagedMaterial managedMaterial)
        {
            return managedMaterial.materialGetter();
        }
    }
}