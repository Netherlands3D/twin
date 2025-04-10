using Object = UnityEngine.Object;

namespace Netherlands3D.Twin.Layers
{
    public static class MaterialApplicator
    {
        /// <summary>
        /// Replaces the material in a way as to prevent mem leaks.
        /// </summary>
        public static void Apply(IMaterialApplicatorAdapter adapter)
        {
            // Get a cached version of the old and new material to prevent issues with creators and setters
            // overwriting the retrieved materials
            var oldMaterial = adapter.GetMaterial();
            var newMaterial = adapter.CreateMaterial();

            // only replace when needed
            if (oldMaterial != newMaterial)
            {
                // Clean up after ourselves - prevents memleaks by releasing C++ materials
                if (oldMaterial) Object.Destroy(oldMaterial);

                // Set explicitly to null to prevent possible C# Managed Shell leaks
                adapter.SetMaterial(null);

                // Apply a newly created material
                adapter.SetMaterial(newMaterial);
            }

            // Set explicitly to null to prevent possible C# Managed Shell leaks
            newMaterial = null;
            oldMaterial = null;
        }
    }
}