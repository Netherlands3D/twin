using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class Inspector : MonoBehaviour
    {
        [SerializeField] private string inspectorClass = "inspector";
        [SerializeField] private PlayableDirector director;
        [SerializeField] private PlayableAsset openingSequence;
        [SerializeField] private PlayableAsset closingSequence;
        [SerializeField] private float switchDelayInSeconds = .3f;

        /// <summary>
        /// Cache for which tool the sidebar is opened, this is used in the Open behaviour to close the
        /// sidebar before reopening it if a prior tool was opened and the tool differs from the new one.
        /// </summary>
        // private string openedForTool => activeTool ? activeTool.code : string.Empty;
        private Tool activeTool;

        private VisualElement inspector;

        private void OnEnable()
        {
            var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            inspector = rootVisualElement.Q(classes: inspectorClass);
        }

        public void Load(Tool tool)
        {
            StartCoroutine(DoLoad(tool));
        }

        private IEnumerator DoLoad(Tool tool)
        {
            var shouldSwitchBetweenTools = activeTool != null && tool.code != activeTool.code;

            if (shouldSwitchBetweenTools)
            {
                Unload(activeTool);
                if (tool.UsesInspector == false) yield break;

                yield return new WaitForSeconds(switchDelayInSeconds);
            }

            inspector.Clear();
            if (tool.UsesInspector == false) yield break;

            tool.InspectorInstance = tool.InspectorTemplate.Instantiate();
            inspector.Add(tool.InspectorInstance);

            activeTool = tool;
            director.playableAsset = openingSequence;
            director.Play();
        }

        public void Unload(Tool tool)
        {
            if (activeTool == null || !string.Equals(tool.code, activeTool.code)) return;

            tool.InspectorInstance = null;
            director.playableAsset = closingSequence;
            director.Play();
            activeTool = null;
        }
    }
}