using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class Inspector : MonoBehaviour
    {
        [SerializeField] private PlayableDirector director;
        [SerializeField] private PlayableAsset openingSequence;
        [SerializeField] private PlayableAsset closingSequence;
        [SerializeField] private float switchDelayInSeconds = .3f;
        
        /// <summary>
        /// Cache for which tool the sidebar is opened, this is used in the Open behaviour to close the
        /// sidebar before reopening it if a prior tool was opened and the tool differs from the new one.
        /// </summary>
        private string openedForTool = null;

        public void Load(Tool tool)
        {
            StartCoroutine(DoLoad(tool));
        }

        private IEnumerator DoLoad(Tool tool)
        {
            var shouldSwitchBetweenTools = 
                string.IsNullOrEmpty(openedForTool) == false 
                && string.Equals(tool.code, openedForTool) == false;
            
            if (shouldSwitchBetweenTools) {
                Unload(openedForTool);
                if (tool.UsesInspector == false) yield break;
        
                yield return new WaitForSeconds(switchDelayInSeconds);
            }

            if (tool.UsesInspector == false) yield break;

            // TODO: Load inspector contents from tool reference
            
            openedForTool = tool.code;
            director.playableAsset = openingSequence;
            director.Play();
        }

        public void Unload(Tool tool)
        {
            Unload(tool.code);
        }

        public void Unload(string tool)
        {
            if (string.IsNullOrEmpty(openedForTool) || string.Equals(tool, openedForTool) == false) return;
            
            director.playableAsset = closingSequence;
            director.Play();
            openedForTool = null;
        }
    }
}
