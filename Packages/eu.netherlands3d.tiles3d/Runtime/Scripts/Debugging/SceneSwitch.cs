namespace Netherlands3D.Tiles3D
{
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class SceneSwitch : MonoBehaviour
    {
        public string targetScene = "DevScene";

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Escape))
            {
                SceneManager.LoadScene(targetScene);
            }
#endif
        }
    }
}
