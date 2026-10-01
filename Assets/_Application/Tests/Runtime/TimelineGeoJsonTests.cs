using Netherlands3D;
using NUnit.Framework;

namespace Netherlands3D.Twin.Tests
{
    public class TimelineGeoJsonTests
    {
        [TestCase("car")]
        [TestCase("bicycle")]
        [TestCase("moped")]
        [TestCase("motorcycle")]
        [TestCase("medium_heavy")]
        [TestCase("heavy")]
        [TestCase("Custom Vehicle Label")]
        public void VehicleDisplayNamePreservesGeoJsonValue(string sourceValue)
        {
            Assert.That(TimelineGeoJson.GetVehicleDisplayName(sourceValue), Is.EqualTo(sourceValue));
        }

        [TestCase("workday")]
        [TestCase("weekday")]
        [TestCase("saturday")]
        [TestCase("sunday")]
        [TestCase("Special service day")]
        public void DayTypeDisplayNamePreservesGeoJsonValue(string sourceValue)
        {
            Assert.That(TimelineGeoJson.GetDayTypeDisplayName(sourceValue), Is.EqualTo(sourceValue));
        }
    }
}
