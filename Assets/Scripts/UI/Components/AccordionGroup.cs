using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI.Components
{
    [RequireComponent(typeof(ToggleGroup))]
    [HelpURL("https://netherlands3d.eu/docs/developers/ui/components/accordion/")]
    public class AccordionGroup : MonoBehaviour
    {
        [Serializable]
        public enum Type
        {
            Normal,
            Single
        }

        [SerializeField] private Type mode = Type.Normal;
        private RectTransform rectTransform;

        private ToggleGroup toggleGroup;
        public ToggleGroup ToggleGroup => toggleGroup;

        public bool OnlySingleSelected => mode == Type.Single;

        private readonly List<Accordion> expandedAccordions = new();
        // Make a copy of the list, we don't want outside forces to manipulate the list; always use the Add and Remove
        // methods in this behaviour
        public List<Accordion> ExpandedAccordions => expandedAccordions.ToList();

        // Keep references to the expand and collapse events to be able to remove the listeners
        private readonly Dictionary<Accordion, UnityAction> childExpandEvents = new();
        private readonly Dictionary<Accordion, UnityAction> childCollapseEvents = new();

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            toggleGroup = GetComponent<ToggleGroup>();
        }

        private void Start()
        {
            toggleGroup.allowSwitchOff = mode == Type.Normal;

            // Register accordion's that are parented to this group; this is done in Start and not in
            // Awake to give the Accordion's a chance to Awake first and set their internals
            foreach (var accordion in GetComponentsInChildren<Accordion>())
            {
                Add(accordion);
            }
        }

        /// <summary>
        /// Adds an accordion to the current group, registers listeners to coordinate collapsing and expanding, and
        /// registers this group onto the accordion.
        /// </summary>
        /// <param name="accordion">The accordion to be added to the group.</param>
        public void Add(Accordion accordion)
        {
            accordion.AddToGroup(this);

            // If the accordion is expanded prior to adding it to this group, make sure it is in our registry
            if (accordion.IsExpanded)
            {
                expandedAccordions.Add(accordion);
            }

            childExpandEvents[accordion] = () => Expand(accordion);
            childCollapseEvents[accordion] = () => Collapse(accordion);
            accordion.onExpand.AddListener(childExpandEvents[accordion]);
            accordion.onCollapse.AddListener(childCollapseEvents[accordion]);
        }

        /// <summary>
        /// Removes an Accordion from the group, and unregisters event listeners.
        /// </summary>
        /// <param name="accordion">The Accordion to be removed.</param>
        public void Remove(Accordion accordion)
        {
            accordion.RemoveFromGroup();
            expandedAccordions.Remove(accordion);
            
            accordion.onExpand.RemoveListener(childExpandEvents[accordion]);
            accordion.onCollapse.RemoveListener(childCollapseEvents[accordion]);
        }

        public void Expand(Accordion accordion)
        {
            if (accordion.Group != this) return;

            // Do not expand again if the group thinks it is expanded to prevent infinite loops
            if (expandedAccordions.Contains(accordion)) return;
            
            if (mode == Type.Single)
            {
                foreach (var expandedAccordion in expandedAccordions.ToList())
                {
                    expandedAccordion.Collapse();
                    expandedAccordions.Remove(expandedAccordion);
                }
            }

            accordion.Expand();
            expandedAccordions.Add(accordion);
            StartCoroutine(ForceLayoutRebuild());
        }

        public void Collapse(Accordion accordion)
        {
            if (accordion.Group != this) return;
            
            // Do not collapse again if the group thinks it is collapsed to prevent infinite loops
            if (!expandedAccordions.Contains(accordion)) return;

            accordion.Collapse();
            expandedAccordions.Remove(accordion);
            StartCoroutine(ForceLayoutRebuild());
        }

        /// <summary>
        /// Forces the layout to be rebuilt by calling
        /// <see cref="LayoutRebuilder.ForceRebuildLayoutImmediate(RectTransform)"/> after waiting for one frame.
        ///
        /// This is done because of Unity issues with nested layout groups where they lag a frame behind when we make
        /// changes in the children of this layout.
        /// </summary>
        private IEnumerator ForceLayoutRebuild()
        {
            // Wait a frame before rebuilding the Layout
            yield return null;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}
