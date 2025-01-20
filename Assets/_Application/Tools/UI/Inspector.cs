using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.Tools.UI
{
    public class Inspector : MonoBehaviour
    {
        public Transform Content => contentPanel;

        [Tooltip("This inspector shows the content panels for these tools")]
        [SerializeField] private Tool[] tools;
        private GameObject currentContent;

        [SerializeField] private Transform contentPanel;
        [SerializeField] private Animator animator;
        [SerializeField] private TMP_Text title;

        [Tooltip("Create and destroy tool content, instead of enabling/disabling existing content panels")]
        [SerializeField] private bool createAndDestroyContent = false;

        void OnEnable()
        {
            //Spawn or despawn prefabs inside tool by subscribing to onActivate and onDeactivate events
            foreach (var tool in tools)
            {
                tool.onToggleInspector.AddListener(Toggled);
            }
        }

        private void OnDisable() {
            foreach (var tool in tools)
            {
                tool.onToggleInspector.RemoveListener(Toggled);
            }
        }

        private void Toggled(Tool tool)
        {
            //Always recreate content (reopen will cause a 'refresh' of the content)
            ClearContent();
            
            if(tool.Open && tool.InspectorPrefab)
            {
                animator.ResetTrigger("Close");
                animator.SetTrigger("Open");
                title.text = tool.title;
   
                if(createAndDestroyContent)
                {
                    currentContent = Instantiate(tool.InspectorPrefab, contentPanel);
                    return;
                }

                //Enable existing content
                foreach (Transform child in contentPanel)
                {
                    if(child.name == tool.InspectorPrefab.name)
                    {
                        currentContent = child.gameObject;
                        currentContent.SetActive(true);
                        return;
                    }
                }
                return;
            }

            animator.ResetTrigger("Open");
            animator.SetTrigger("Close");
        }

        private void ClearContent()
        {
            if(currentContent == null) return;

            currentContent.SetActive(false);
            if(createAndDestroyContent)
            {
                Destroy(currentContent);
            }
        }
    }
}
