using System.Collections;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Tests.PageObjectModel;
using UnityEngine;

namespace Netherlands3D.Twin.Tests
{
    public abstract class TestCase : E2ETesting.TestCase
    {
        internal Sidebar Sidebar { get; private set; }
        
        internal Scene Scene { get; private set; }

        private bool projectIsLoaded;

        //setup the test-environment
        
    }
}