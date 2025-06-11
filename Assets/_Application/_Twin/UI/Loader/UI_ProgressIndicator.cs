using UnityEngine;
using System.Collections;

namespace Netherlands3D.Twin.UI.Loader
{
    public class UI_ProgressIndicator : MonoBehaviour
    {
        [SerializeField] private RectTransform fillBar;
        [SerializeField] private bool logInConsole;
        public GameObject StartCanvas;

        private float currentProgress = 0.0f;

        private void Awake()
        {
            fillBar.offsetMax = new Vector2(fillBar.offsetMax.x, fillBar.offsetMax.y);
            StartCanvas = GameObject.FindGameObjectWithTag("MainCanvas");
        }

        private void LateUpdate()
        {
            if (currentProgress >= 1f || currentProgress <= 0f)
           
                this.gameObject.SetActive(false);

            try
            {
                StartCanvas.GetComponent<Animator>().SetTrigger("Start");
            }
            catch
            {
                print("error");
            }

        }

        public void ShowProgress(float progress)
        {
            this.gameObject.SetActive(true);
            currentProgress = progress;

            if (logInConsole) Debug.Log($"<color=#0000FF>Progress:{progress}");

            fillBar.localScale = new Vector3(Mathf.Abs(progress), 1, 1);
        }
    }
}