using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class DashboardProjectOverview : VisualElement
    {
        private const string EmptyValue = "\u2014";

        private readonly Label totalDwellings;
        private readonly Label blockCount;
        private readonly Label nonHousingGrossFloorArea;
        private readonly Label socialShare;

        public DashboardProjectOverview()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            totalDwellings = this.Q<Label>("ProjectDwellings");
            blockCount = this.Q<Label>("ProjectBlocks");
            nonHousingGrossFloorArea = this.Q<Label>("ProjectNonHousingGrossFloorArea");
            socialShare = this.Q<Label>("ProjectSocialShare");
        }

        internal void SetData(string dwellings, string blocks, string grossFloorArea, string share)
        {
            totalDwellings.text = DisplayValue(dwellings);
            blockCount.text = DisplayValue(blocks);
            nonHousingGrossFloorArea.text = DisplayValue(grossFloorArea);
            socialShare.text = DisplayValue(share);
        }

        private static string DisplayValue(string value) => string.IsNullOrWhiteSpace(value) ? EmptyValue : value;
    }
}
