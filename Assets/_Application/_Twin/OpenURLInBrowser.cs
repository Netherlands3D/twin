using UnityEngine;

namespace Netherlands3D.Twin
{
    public class OpenURLInBrowser : MonoBehaviour
    {
        [field:SerializeField]
        public string UrlToOpen { get; set; }

        public void Open()
        {
            Open(UrlToOpen);
        }

        public void Open(string url)
        {
            Application.OpenURL(url);
        }
    }
}
