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
            Replace(materialCreator, materialGetter, materialSetter);
        }

        /// <summary>
        /// Replaces the material in a way as to prevent mem leaks.
        ///
        /// It is recommended to wrap a material use by instantiating this method and calling
        /// UpdateMaterial, though there are advanced use cases where you want to use state to manipulate the
        /// process. This will help in these situations.
        /// </summary>
        public static void Replace(
            Func<Material> materialCreator,
            Func<Material> materialGetter,
            Action<Material> materialSetter
        ) {
            // Get a cached version of the old and new material to prevent issues with creators and setters
            // overwriting the retrieved materials
            var oldMaterial = materialGetter();
            var newMaterial = materialCreator();

            // only replace when needed
            if (oldMaterial != newMaterial)
            {
                // Clean up after ourselves - prevents memleaks by releasing C++ materials
                if (oldMaterial) Object.Destroy(oldMaterial);

                // Set explicitly to null to prevent possible C# Managed Shell leaks
                materialSetter(null);

                // Apply a newly created material
                materialSetter(newMaterial);
            }

            // Set explicitly to null to prevent possible C# Managed Shell leaks
            newMaterial = null;
            oldMaterial = null;
        }
    }
}