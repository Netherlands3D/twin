using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit.Scripts;
using System.Security.Cryptography.X509Certificates;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class SearchBar : VisualElement
    {
        private const string DefaultPlaceholder = "Zoeken naar adres";
        private const string OpenClass = "search-bar--open";

        private TextField queryField;
        private IconButton searchButton;
        private VisualElement resultsContainer;
        private ListView resultsList;

        // Backing field so inspector overrides persist and are applied on attach.
        private string placeholderText = DefaultPlaceholder;
        private bool isOpen;

        public TextField QueryField => queryField ??= this.Q<TextField>("QueryField");
        public IconButton SearchButton => searchButton ??= this.Q<IconButton>("SearchButton");
        public VisualElement ResultsContainer => resultsContainer ??= this.Q<VisualElement>("ResultsContainer");
        public ListView ResultsList => resultsList ??= this.Q<ListView>("ResultsList");

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

        public SearchBar()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            AddToClassList("search-bar");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                EnableInClassList(OpenClass, isOpen);
                ApplyPlaceholder();
            });
        }

        private void ApplyPlaceholder()
        {
            if (QueryField?.textEdition == null) return;

            // Unity 6: placeholder on textEdition.
            QueryField.textEdition.placeholder = placeholderText;
        }
    }
}