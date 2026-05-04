using System;
using System.Collections.Generic;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class AutoComplete : VisualElement
    {
        private const string DefaultPlaceholder = "";
        private const string OpenClass = "auto-complete--open";

        private TextField queryField;
        private TextField QueryField => queryField ??= this.Q<TextField>("QueryField");

        private IconButton searchButton;
        private IconButton SearchButton => searchButton ??= this.Q<IconButton>("SearchButton");

        private ListView resultsList;
        private ListView ResultsList => resultsList ??= this.Q<ListView>("ResultsList");

        private bool isOpen;

        [UxmlAttribute("is-open")]
        public bool IsOpen
        {
            get => isOpen;
            set
            {
                isOpen = value;
                EnableInClassList(OpenClass, value);
            }
        }

        private string placeholderText = DefaultPlaceholder;

        [UxmlAttribute("placeholder-text")]
        public string PlaceholderText
        {
            get => placeholderText;
            set
            {
                placeholderText = value ?? DefaultPlaceholder;
                ApplyPlaceholder();
            }
        }

        public event Action<string> QueryChanged;
        public event Action<string, int?> SubmitRequested;
        public event Action<int> ResultActivated;

        private readonly List<object> resultItems = new();
        private Func<object, string> resultLabelSelector = item => item?.ToString() ?? string.Empty;
        private int selectedResultIndex = -1;
        private bool callbacksRegistered;

        public AutoComplete()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                EnableInClassList(OpenClass, isOpen);
                ApplyPlaceholder();
                SetupCallbacks();
            });
        }

        public void SetQueryText(string text)
        {
            QueryField.SetValueWithoutNotify(text ?? string.Empty);
        }

        public void SetResults<T>(IReadOnlyList<T> items, Func<T, string> labelSelector)
        {
            resultItems.Clear();

            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                    resultItems.Add(items[i]);
            }

            resultLabelSelector = labelSelector == null
                ? item => item?.ToString() ?? string.Empty
                : item => labelSelector((T)item);

            selectedResultIndex = -1;
            ResultsList.itemsSource = resultItems;
            ResultsList.Rebuild();
            IsOpen = resultItems.Count > 0;
            ResultsList.ClearSelection();
        }

        public void ClearResults()
        {
            resultItems.Clear();
            selectedResultIndex = -1;
            ResultsList.itemsSource = resultItems;
            ResultsList.Rebuild();
            IsOpen = false;
        }

        private void SetupCallbacks()
        {
            if (callbacksRegistered) return;
            callbacksRegistered = true;

            QueryField.RegisterValueChangedCallback(evt => QueryChanged?.Invoke(evt.newValue));
            QueryField.RegisterCallback<NavigationSubmitEvent>(OnQuerySubmit, TrickleDown.TrickleDown);
            QueryField.RegisterCallback<KeyDownEvent>(OnQueryFieldKeyDown, TrickleDown.TrickleDown);

            SearchButton.RegisterCallback<ClickEvent>(_ =>
            {
                int? selected = resultItems.Count == 0 || selectedResultIndex < 0 ? null : selectedResultIndex;
                SubmitRequested?.Invoke(QueryField.value, selected);
            });

            ResultsList.makeItem = MakeResultItem;
            ResultsList.bindItem = BindResultItem;
            ResultsList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ResultsList.selectionType = SelectionType.Single;
        }

        private void OnQuerySubmit(NavigationSubmitEvent _)
        {
            int? selected = resultItems.Count == 0 || selectedResultIndex < 0 ? null : selectedResultIndex;
            SubmitRequested?.Invoke(QueryField.value, selected);
        }

        private void OnQueryFieldKeyDown(KeyDownEvent evt)
        {
            if (resultItems.Count == 0) return;

            int newSelection;
            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    newSelection = selectedResultIndex < 0 ? 0 : selectedResultIndex + 1;
                    break;
                case KeyCode.UpArrow:
                    newSelection = selectedResultIndex < 0 ? resultItems.Count - 1 : selectedResultIndex - 1;
                    break;
                default:
                    return;
            }

            ChangeSelectionTo(newSelection);
            evt.StopPropagation();
        }

        private void ChangeSelectionTo(int selectionIndex)
        {
            selectedResultIndex = Mathf.Clamp(selectionIndex, 0, resultItems.Count - 1);
            ResultsList.SetSelectionWithoutNotify(new[] { selectedResultIndex });
        }

        private VisualElement MakeResultItem()
        {
            var item = new ListViewItem();
            var label = new Label();
            label.AddToClassList("label");
            item.Add(label);
            item.RegisterCallback<ClickEvent>(OnResultItemClicked);

            return item;
        }

        private void BindResultItem(VisualElement element, int index)
        {
            if (index < 0 || index >= resultItems.Count) return;

            var label = element.Q<Label>();
            if (label != null)
                label.text = resultLabelSelector(resultItems[index]);

            element.userData = index;
        }

        private void OnResultItemClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not VisualElement item || item.userData is not int index) return;
            if (index < 0 || index >= resultItems.Count) return;

            selectedResultIndex = index;
            ResultActivated?.Invoke(index);
        }

        private void ApplyPlaceholder()
        {
            if (QueryField?.textEdition == null) return;
            QueryField.textEdition.placeholder = placeholderText;
        }
    }
}

