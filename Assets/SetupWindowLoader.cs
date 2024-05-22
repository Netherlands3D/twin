using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration
{
    public class SetupWindowLoader : MonoBehaviour
    {
        [SerializeField] private Configurator configurator;
        private void Start()
        {
            if (configurator.Configuration.ShouldStartSetup)
            {
                OpenWindow();
            }
        }

        public void OpenWindow()
        {
            configurator.StartSetup();
        }
    }
}
