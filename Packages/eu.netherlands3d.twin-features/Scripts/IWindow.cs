using UnityEngine.Events;

namespace Netherlands3D.Twin.Features
{
    public interface IWindow
    {
        void Open();
        void Close();

        public UnityEvent OnOpen{ get; }
        public UnityEvent OnClose{ get; }

        public bool IsOpen
        {
            get;
            set;
        }
    }
}