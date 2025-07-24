using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Netherlands3D.Twin
{
    public class Logger : MonoBehaviour, ILogHandler 
    {
        private ILogHandler decoratedLogHandler;
        [field: SerializeField] public bool IsEnabled { get; private set; } = true;

        private void Awake()
        {
            this.decoratedLogHandler = Debug.unityLogger.logHandler; 
            Debug.unityLogger.logHandler = this;
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (IsEnabled) decoratedLogHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, Object context)
        {
            if (IsEnabled) decoratedLogHandler.LogException(exception, context);
        }
    }
}