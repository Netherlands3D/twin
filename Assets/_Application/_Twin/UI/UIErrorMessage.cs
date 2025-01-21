using UnityEngine;

namespace Netherlands3D.Twin.UI
{
    public class UIErrorMessage : MonoBehaviour
    {
        public GameObject [] ToHide;
        public GameObject [] ToShow;

        private void OnEnable()
        {
            foreach(GameObject hide in ToHide)
            {
                hide.SetActive(false);
            }

            foreach (GameObject show in ToShow)
            {
                show.SetActive(true);
            }
        }      
    }
}
