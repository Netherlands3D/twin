using KindMen.Uxios.Interceptors.NetworkInspector;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class NetworkSettings : MonoBehaviour
    {
        [SerializeField] private bool logNetworkCalls;
        
        private ConsoleLogger networkLogger = null;

        public bool LogNetworkCalls
        {
            get => logNetworkCalls;
            set
            {
                logNetworkCalls = value;
                if (value) 
                    EnableLogging();
                else 
                    DisableLogging();
            }
        }

        private void Awake()
        {
            if (LogNetworkCalls)
            {
                this.networkLogger = new ConsoleLogger();
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Change the application behaviour based on the change in the private bools. This method is surrounded by
        /// a UNITY_EDITOR pragma because production should only listen to the initial value (see Awake).
        /// </summary>
        private void Update()
        {
            // Actively toggle the Console logger during playmode if the boolean was toggled from
            // the editor
            switch (LogNetworkCalls)
            {
                case true: EnableLogging(); break;
                case false: DisableLogging(); break;
            }
        }

        private void EnableLogging()
        {
            if (networkLogger != null) return;

            this.networkLogger = new ConsoleLogger();
        }

        private void DisableLogging()
        {
            if (networkLogger == null) return;
            
            this.networkLogger.Dispose();
            this.networkLogger = null;
        }
#endif
    }
}
