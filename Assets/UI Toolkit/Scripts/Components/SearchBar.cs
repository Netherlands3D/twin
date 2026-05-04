using System;
using System.Collections.Generic;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class SearchBar : VisualElement
    {
        private const string DefaultPlaceholder = "Zoeken naar adres";
        private const string OpenClass = "search-bar--open";

        private TextField queryField;
        private TextField QueryField => queryField ??= this.Q<TextField>("QueryField");

        private IconButton searchButton;
        private IconButton SearchButton => searchButton ??= this.Q<IconButton>("SearchButton");

        private ListView resultsList;
        private ListView ResultsList => resultsList ??= this.Q<ListView>("ResultsList");

        private bool isOpen;

        /// <summary>
        /// Controls visibility of the results container via the "search-bar--open" state class.
        /// </summary>
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

        // Backing field so inspector overrides persist and are applied on attach.
        private string placeholderText = DefaultPlaceholder;

        /// <summary>
        /// Placeholder text for the query field. Can be overridden in UI Builder Inspector.
        /// If not set, defaults to "Zoeken naar adres".
        /// </summary>
        [UxmlAttribute("placeholder-text")]
        public string PlaceholderText
        {
            get => placeholderText;
            set
            {
                placeholderText = string.IsNullOrWhiteSpace(value) ? DefaultPlaceholder : value;
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

        public SearchBar()
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
                {
                    resultItems.Add(items[i]);
                }
            }

            resultLabelSelector = labelSelector == null
                ? item => item?.ToString() ?? string.Empty
                : item => labelSelector((T)item);

            selectedResultIndex = resultItems.Count > 0 ? 0 : -1;
            ResultsList.itemsSource = resultItems;
            ResultsList.Rebuild();
            IsOpen = resultItems.Count > 0;

            if (selectedResultIndex >= 0)
            {
                ResultsList.SetSelectionWithoutNotify(new[] { selectedResultIndex });
            }
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
                int? selected = resultItems.Count == 0 ? null : selectedResultIndex;
                SubmitRequested?.Invoke(QueryField.value, selected);
            });

            ResultsList.makeItem = MakeResultItem;
            ResultsList.bindItem = BindResultItem;
            ResultsList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            ResultsList.selectionType = SelectionType.Single;
        }

        private void OnQuerySubmit(NavigationSubmitEvent _)
        {
            int? selected = resultItems.Count == 0 ? null : selectedResultIndex;
            SubmitRequested?.Invoke(QueryField.value, selected);
        }

        private void OnQueryFieldKeyDown(KeyDownEvent evt)
        {
            if (resultItems.Count == 0) return;

            int newSelection = 0;
            switch (evt.keyCode)
            {
                case KeyCode.DownArrow: newSelection = selectedResultIndex + 1; break;
                case KeyCode.UpArrow: newSelection = selectedResultIndex - 1; break;
                // Don't change selection if the key is not recognized
                default: return;
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
            {
                label.text = resultLabelSelector(resultItems[index]);
            }

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