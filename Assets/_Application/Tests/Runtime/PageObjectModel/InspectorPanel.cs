using Netherlands3D.E2ETesting.PageObjectModel;

namespace Netherlands3D.Twin.Tests.PageObjectModel
{
    public class InspectorPanel<T> : Element<T> where T : UnityEngine.Object  
    {
        public InspectorPanel(T value) : base(value)
        {
        }

        public bool IsOpen => IsActive;
    }
}