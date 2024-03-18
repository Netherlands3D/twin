using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI.Components
{
    [HelpURL("https://netherlands3d.eu/docs/developers/ui/components/accordion/")]
    public class Accordion : MonoBehaviour
    {
        [Serializable]
        private enum State
        {
            Collapsed,
            Expanded
        }
        [SerializeField] private Image selectedBackground;
        [SerializeField] private Sprite collapsedIcon;
        [SerializeField] private Sprite expandedIcon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private GameObject divider;
        [SerializeField] private GameObject content;
        [SerializeField] private State state;
        public bool IsExpanded => state == State.Expanded;
        public bool IsCollapsed => state == State.Collapsed;
        public AccordionGroup Group { get; private set; }

        private Toggle toggle;

        public UnityEvent onExpand = new();
        public UnityEvent onCollapse = new();

        private void Awake()
        {
            toggle = transform.GetComponentInChildren<Toggle>();
            toggle.onValueChanged.AddListener(ToggleTo);
        }

        private void Start()
        {
            UpdateUI();
        }

        private void ToggleTo(bool value)
        {
            if (value)
            {
                Expand();
                return;
            }

            Collapse();
        }

        public void Expand()
        {
            if (state == State.Expanded) return;
            
            state = State.Expanded;

            UpdateUI();
            onExpand.Invoke();
        }

        public void Collapse()
        {
            if (state == State.Collapsed) return;
            
            state = State.Collapsed;

            UpdateUI();
            onCollapse.Invoke();
        }

        private void UpdateUI()
        {
            title.fontWeight = IsExpanded ? FontWeight.Bold : FontWeight.Regular;
            toggle.SetIsOnWithoutNotify(IsExpanded);
            var toggleImage = toggle.targetGraphic as Image;
            if (toggleImage != null) toggleImage.sprite = IsExpanded ? expandedIcon : collapsedIcon;
            content.SetActive(IsExpanded);
            divider.SetActive(IsExpanded);
            
            // Accordions belonging to a group in 'Single' mode should show a selection box around the accordion
            // and hide the collapse icon.
            var isExpandedAndShouldBeSingle = Group && Group.OnlySingleSelected && this.IsExpanded;
            toggle.group = Group && Group.OnlySingleSelected ? Group.ToggleGroup : null;
            selectedBackground.enabled = isExpandedAndShouldBeSingle;
            toggle.interactable = !isExpandedAndShouldBeSingle;
            toggle.targetGraphic.enabled = !isExpandedAndShouldBeSingle;
        }

        public void AddToGroup(AccordionGroup accordionGroup)
        {
            Group = accordionGroup;
            UpdateUI();
        }

        public void RemoveFromGroup()
        {
            Group = null;
            UpdateUI();
        }
    }
}
