using UnityEngine;

namespace Netherlands3D.Twin
{
    public class OpenURLInBrowser : MonoBehaviour
    {
        public void Open(string url)
        {
            Application.OpenURL(url);
        }
    }
}
