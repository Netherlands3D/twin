using System.Collections;
using UnityEngine.TestTools;

namespace Netherlands3D.E2ETesting
{
    public abstract class TestCase
    {
        [UnitySetUp]
        public virtual IEnumerator TestSetup()
        {
            yield return E2E.EnsureMainSceneIsLoaded();
        }
    }
}