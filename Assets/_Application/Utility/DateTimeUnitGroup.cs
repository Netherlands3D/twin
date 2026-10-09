namespace Netherlands3D.Twin.Utility
{
    public struct DateTimeUnitGroup
    {

        public DateTimeUnitGroup(DateTimeUnit timeUnit, float timeUnitFactor)
        {
            this.TimeUnit = timeUnit;
            this.TimeUnitFactor = timeUnitFactor;
        }

        public DateTimeUnit TimeUnit { get; private set; }
        public float TimeUnitFactor { get; private set; }
    }
}