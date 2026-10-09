namespace Netherlands3D.Twin.Utility
{
    public struct DateTimeInterval
    {

        public DateTimeInterval(DateTimeUnit timeUnit, double amount)
        {
            this.TimeUnit = timeUnit;
            this.Amount = amount;
        }

        public DateTimeUnit TimeUnit { get; private set; }
        public double Amount { get; private set; }
    }
}