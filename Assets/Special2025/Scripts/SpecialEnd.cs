using UnityEngine;

namespace Netherlands3D
{
    public class SpecialEnd : MonoBehaviour
    {
        [SerializeField] private GameObject startScreen;
        [SerializeField] private GameObject endScreen;

        public void GoBack()
        {
            endScreen.SetActive(false);
        }

        public void CreateANewOne()
        {
            endScreen.SetActive(false);
            startScreen.SetActive(true);
        }

        public void ShareImage()
        {

        }
    }
}
