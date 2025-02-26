using System.Collections;
using UnityEngine.TestTools;

namespace Netherlands3D
{
    public abstract class E2ETestCase
    {
        [UnitySetUp]
        public virtual IEnumerator LoadSceneOnce()
        {
            yield return E2E.EnsureMainSceneIsLoaded();
        }
    }
}