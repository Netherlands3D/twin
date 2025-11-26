using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class PropertySectionRegistryBuildProcessor : IPreprocessBuildWithReport //todo is this needed?
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            // Automatically rebuild registry before the build
            PropertySectionRegistryBuilder.Rebuild(true); 
        }
    }
}