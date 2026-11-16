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

        public string HeaderText
        {
            get => header.LabelText;
            set => header.LabelText = value;
        }

        public string TotalDwellings
        {
            get => totalDwellingsValue.text;
            set => SetValue(totalDwellingsValue, value);
        }

        public string SocialDwellings
        {
            get => socialDwellingsValue.text;
            set => SetValue(socialDwellingsValue, value);
        }

        public string MidRentDwellings
        {
            get => midRentDwellingsValue.text;
            set => SetValue(midRentDwellingsValue, value);
        }

        public string FreeSectorDwellings
        {
            get => freeSectorDwellingsValue.text;
            set => SetValue(freeSectorDwellingsValue, value);
        }

        public string AverageMinimumFloorArea
        {
            get => averageMinimumFloorAreaValue.text;
            set => SetValue(averageMinimumFloorAreaValue, value);
        }

        public string AvailablePhases
        {
            get => availablePhasesValue.text;
            set => SetValue(availablePhasesValue, value);
        }

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
