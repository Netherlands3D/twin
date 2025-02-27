using System.Collections;
using Netherlands3D.Twin.Tests.PageObjectModel;

namespace Netherlands3D.Twin.Tests
{
    public abstract class TestCase : E2ETesting.TestCase
    {
        internal Sidebar Sidebar { get; private set; }
        
        internal WorldView WorldView { get; private set; }

        public override IEnumerator LoadSceneOnce()
        {
            yield return base.LoadSceneOnce();
            
            this.Sidebar = new Sidebar();
            this.WorldView = new WorldView();
        }
    }
}