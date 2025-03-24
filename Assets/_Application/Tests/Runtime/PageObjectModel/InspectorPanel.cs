using Netherlands3D.E2ETesting.PageObjectModel;

namespace Netherlands3D.Twin.Tests.PageObjectModel
{
    public abstract class InspectorPanel<T, TDerived> : Element<T, TDerived> 
        where T : UnityEngine.Object  
        where TDerived : Element<T, TDerived>, new()
    {
        public bool IsOpen => IsActive;
    }
}