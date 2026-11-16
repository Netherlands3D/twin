using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using Netherlands3D.UI_Toolkit;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DashboardBlockOverview : VisualElement
    {
        private const string EmptyValue = "\u2014";

        private readonly Header header;
        private readonly VisualElement rows;
        private readonly Label emptyState;
        private readonly List<Label> valueLabels;
        private readonly Label totalDwellingsValue;
        private readonly Label socialDwellingsValue;
        private readonly Label midRentDwellingsValue;
        private readonly Label freeSectorDwellingsValue;
        private readonly Label averageMinimumFloorAreaValue;
        private readonly Label availablePhasesValue;
        private readonly Label nonHousingGrossFloorAreaValue;

        [UxmlAttribute("header-text")]
        public string HeaderText
        {
            get => header.LabelText;
            set => header.LabelText = value;
        }

        [UxmlAttribute("total-dwellings")]
        public string TotalDwellings
        {
            get => totalDwellingsValue.text;
            set => SetValue(totalDwellingsValue, value);
        }

        [UxmlAttribute("social-dwellings")]
        public string SocialDwellings
        {
            get => socialDwellingsValue.text;
            set => SetValue(socialDwellingsValue, value);
        }

        [UxmlAttribute("mid-rent-dwellings")]
        public string MidRentDwellings
        {
            get => midRentDwellingsValue.text;
            set => SetValue(midRentDwellingsValue, value);
        }

        [UxmlAttribute("free-sector-dwellings")]
        public string FreeSectorDwellings
        {
            get => freeSectorDwellingsValue.text;
            set => SetValue(freeSectorDwellingsValue, value);
        }

        [UxmlAttribute("average-minimum-floor-area")]
        public string AverageMinimumFloorArea
        {
            get => averageMinimumFloorAreaValue.text;
            set => SetValue(averageMinimumFloorAreaValue, value);
        }

        [UxmlAttribute("available-phases")]
        public string AvailablePhases
        {
            get => availablePhasesValue.text;
            set => SetValue(availablePhasesValue, value);
        }

        [UxmlAttribute("non-housing-gross-floor-area")]
        public string NonHousingGrossFloorArea
        {
            get => nonHousingGrossFloorAreaValue.text;
            set => SetValue(nonHousingGrossFloorAreaValue, value);
        }

        public DashboardBlockOverview()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            header = this.Q<Header>("BlockHeader");
            rows = this.Q("BlockRows");
            emptyState = this.Q<Label>("EmptyBlockOverview");
            valueLabels = rows.Query<Label>(className: "dashboard-block-overview__value").ToList();
            totalDwellingsValue = this.Q<Label>("TotalDwellingsValue");
            socialDwellingsValue = this.Q<Label>("SocialDwellingsValue");
            midRentDwellingsValue = this.Q<Label>("MidRentDwellingsValue");
            freeSectorDwellingsValue = this.Q<Label>("FreeSectorDwellingsValue");
            averageMinimumFloorAreaValue = this.Q<Label>("AverageMinimumFloorAreaValue");
            availablePhasesValue = this.Q<Label>("AvailablePhasesValue");
            nonHousingGrossFloorAreaValue = this.Q<Label>("NonHousingGrossFloorAreaValue");
        }

        private void SetValue(Label label, string value)
        {
            label.text = string.IsNullOrWhiteSpace(value) ? EmptyValue : value;

            var hasData = valueLabels.Any(valueLabel => valueLabel.text != EmptyValue);
            rows.EnableInClassList(UtilityClassConstants.HIDDEN, !hasData);
            emptyState.EnableInClassList(UtilityClassConstants.HIDDEN, hasData);
        }
    }
}
