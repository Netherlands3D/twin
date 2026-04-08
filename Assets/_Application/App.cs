using Netherlands3D.Services;
using Netherlands3D.Twin.Services;

namespace Netherlands3D.Twin
{
    /// <summary>
    /// Provides static access to application-wide facade services utilized in the Netherlands3D project.
    /// Primarily used to access and manage layers through a service locator.
    /// </summary>
    /// <seealso href="https://refactoring.guru/design-patterns/facade" />
    public static class App
    {
        public static ILayersServiceFacade Layers => ServiceLocator.GetService<Services.Layers>();
        public static Cameras.CameraService Cameras => ServiceLocator.GetService<Cameras.CameraService>();
    }
}
