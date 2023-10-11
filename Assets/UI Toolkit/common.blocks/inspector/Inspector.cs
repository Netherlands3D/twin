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
        
        /// <summary>
        /// Cache for which tool the sidebar is opened, this is used in the Open behaviour to close the
        /// sidebar before reopening it if a prior tool was opened and the tool differs from the new one.
        /// </summary>
        private string openedForTool = null;

        public void Open(string tool)
        {
            if (
                string.IsNullOrEmpty(openedForTool) == false 
                && string.Equals(tool, openedForTool) == false
            ) {
                Close(openedForTool);
            }

            openedForTool = tool;
            director.playableAsset = openingSequence;
            director.Play();
        }

        public void Close(string tool)
        {
            if (string.Equals(tool, openedForTool) == false) return;
            
            director.playableAsset = closingSequence;
            director.Play();
            openedForTool = null;
        }
    }
}
