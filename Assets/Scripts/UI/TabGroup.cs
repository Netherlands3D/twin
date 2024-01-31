using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.UI
{
    [RequireComponent(typeof(HorizontalOrVerticalLayoutGroup))]
    public class TabGroup : MonoBehaviour
    {
        [Serializable]
        public struct TabGroupColors
        {
            public TabButtonColors normal;
            public TabButtonColors selected;
            public TabButtonColors hover;
        }

        [Serializable]
        public struct TabButtonColors
        {
            public Color background;
            public Color outline;
            public Color text;
        }
        
        private List<TabButton> tabButtons = new();
        public int outlineWidth = 2;
        public TabGroupColors colors = new();
        public TabButton selectedTabButton;

        private void Start()
        {
            var layoutGroup = GetComponent<HorizontalOrVerticalLayoutGroup>();
            layoutGroup.padding.bottom = outlineWidth;
            layoutGroup.padding.top = outlineWidth;
            layoutGroup.padding.left = outlineWidth;
            layoutGroup.padding.right = outlineWidth;
            layoutGroup.spacing = outlineWidth + 1;

            foreach (var tabButton in GetComponentsInChildren<TabButton>())
            {
                Subscribe(tabButton);
            }
        }

        public void Subscribe(TabButton tabButton)
        {
            tabButtons.Add(tabButton);
            if (!selectedTabButton)
            {
                ActivateTab(tabButton);
                return;
            }
            
            if (selectedTabButton && selectedTabButton == tabButton)
            {
                // The active tab method has a guard that you cannot activate an already active tab; but in this case
                // the tab hadn't been initialized yet, so we cheat by unsetting the selected tab button field for a
                // moment
                selectedTabButton = null;
                ActivateTab(tabButton);
                return;
            }

            DeactivateTab(tabButton);
        }

        public void OnTabEnter(TabButton tabButton)
        {
            // Never update the style of the selected tab as it will undo the visual styling for being active
            if (selectedTabButton == tabButton) return;

            tabButton.SetOutline(outlineWidth, colors.hover.outline);
            tabButton.SetColors(colors.hover.background, colors.hover.text);
        }

        public void OnTabExit(TabButton tabButton)
        {
            DeactivateTab(tabButton);
        }

        public void OnTabSelected(TabButton tabButton)
        {
            ActivateTab(tabButton);
        }

        public void ActivateTab(TabButton tabButton)
        {
            // We don't want to reactive the active tab; you can click on this as much as you want but it opens once
            if (selectedTabButton == tabButton) return;
            
            var previousTabButton = selectedTabButton;
            selectedTabButton = tabButton;

            // If there was a previous tab selected: make sure it is deactivated along with its pane
            if (previousTabButton)
            {
                DeactivateTab(previousTabButton);
            }

            if (tabButton.tabPane) tabButton.tabPane.SetActive(true);
            tabButton.SetOutline(outlineWidth, colors.selected.outline);
            tabButton.SetColors(colors.selected.background, colors.selected.text);
        }
        
        public void DeactivateTab(TabButton tabButton)
        {
            // Make sure we never deactivate the active tab; one tab must always be active
            if (selectedTabButton == tabButton) return;

            if (tabButton.tabPane) tabButton.tabPane.SetActive(false);
            tabButton.SetOutline(outlineWidth, colors.normal.outline);
            tabButton.SetColors(colors.normal.background, colors.normal.text);
        }
    }
}
