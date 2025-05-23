using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI
{
    public class Dialog : MonoBehaviour
    {
        [SerializeField] private Button confirm;
        [SerializeField] private Button cancel;

        public UnityEvent Cancel = new();
        public UnityEvent Confirm = new();
        public UnityEvent Close = new();

        private void Start()
        {
            confirm.onClick.AddListener(() =>
            {
                Show(false);
                Confirm.Invoke();
            });
            cancel.onClick.AddListener(() =>
            {
                Show(false);
                Cancel.Invoke();
            });
        }

        public void Show(bool show)
        {
            gameObject.SetActive(show);
            if(!show)
                Close.Invoke();
        }
    }
}
