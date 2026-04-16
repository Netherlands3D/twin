using UnityEngine;

namespace Netherlands3D.Twin
{
    public class OpenURLInBrowser : MonoBehaviour //todo: delete this class when transition to UI toolkit is complete
    {
        public void Open(string url)
        {
            Application.OpenURL(url);
        }
    }
}
