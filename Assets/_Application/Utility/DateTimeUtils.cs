using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Utility
{
    public static class DateTimeUtils
    {

        public static DateTime ToDateTime(this Length length, DateTime startTime, DateTime endTime, double ticksPerPixel)
        {
            if (length.unit == LengthUnit.Pixel)
            {
                return length.ToDateTime(startTime, ticksPerPixel);
            } else if (length.unit == LengthUnit.Percent)
            {
                return length.ToDateTime(startTime, endTime);
            }
            throw new Exception();
        }

        public static DateTime ToDateTime(this Length length, DateTime startTime, double ticksPerPixel)
        {
            if (length.unit != LengthUnit.Pixel)
            {
                throw new Exception();
            }
            return startTime.AddTicks((long)Math.Round(length.value * ticksPerPixel));
        }
        
        public static DateTime ToDateTime(this Length length, DateTime startTime, DateTime endTime)
        {
            if (length.unit != LengthUnit.Percent)
            {
                throw new Exception();
            }
            return startTime.Add((endTime - startTime)*length.value*0.01d);
        }

        public static Length ToLength(this DateTime time, DateTime startTime, double ticksPerPixel)
        {
            return new Length();
        }
        
        
        public static double GetDateTimeUnitCountBetween(this DateTimeUnit timeUnit, DateTime startTime, DateTime endTime, double timeUnitFactor = 1)
        {
            var unitCount = timeUnit switch
            {
                DateTimeUnit.Century => GetYears() / 100d,
                DateTimeUnit.Decade => GetYears() / 10d,
                DateTimeUnit.Year => GetYears(),
                DateTimeUnit.Quarter => GetMonths() / 4d,
                DateTimeUnit.Month => GetMonths(),
                DateTimeUnit.Day => (endTime - startTime).TotalDays,
                DateTimeUnit.Hour => (endTime - startTime).TotalHours,
                DateTimeUnit.Minute => (endTime - startTime).TotalMinutes,
                DateTimeUnit.Second => (endTime - startTime).TotalSeconds,
                _ => throw new ArgumentOutOfRangeException(nameof(timeUnit))
            };

            return unitCount / timeUnitFactor;
            
            double GetYears() {
                var count = endTime.Year - startTime.Year;
                if (startTime.AddYears(count) > endTime) count--;

                var previous = startTime.AddYears(count);
                var next = startTime.AddYears(count + 1);

                return count + (endTime - previous).TotalDays / (next - previous).TotalDays;
            }
            
            double GetMonths()
            {
                var count = (endTime.Year - startTime.Year) * 12 + endTime.Month - startTime.Month;
                if (startTime.AddMonths(count) > endTime) count--;

                var previous = startTime.AddMonths(count);
                var next = startTime.AddMonths(count + 1);

                return count + (endTime - previous).TotalDays / (next - previous).TotalDays;
            }
        }

        public static List<DateTime> GetDateTimesBetween(DateTime startTime, DateTime endTime, DateTimeInterval interval)
        {
            var results = new List<DateTime>();
            GetDateTimesBetweenNonAlloc(results, startTime, endTime, interval);
            return results;
        }

        public static void GetDateTimesBetweenNonAlloc(List<DateTime> results, DateTime startTime, DateTime endTime, DateTimeInterval interval)
        {
            results.Clear();

            var anchor = startTime.RoundDown(interval);

            for (var i = 0; ; i++)
            {
                var time = anchor.Add(new DateTimeInterval(interval.TimeUnit, interval.Amount * i));
                if (time >= endTime)
                {
                    break;
                }

                if (time >= startTime && (results.Count == 0 || results[^1] != time))
                {
                    results.Add(time);
                }
            }
        }
        
        
        /// <returns>Returns the amount of unit groups that fit between startDateTime and endDateTime.</returns>
        private static double GetUnitGroupCount(DateTimeInterval dateTimeInterval, DateTime startTime, DateTime endTime)
        {
            var unitGroupCount = dateTimeInterval.TimeUnit.GetDateTimeUnitCountBetween(startTime, endTime, dateTimeInterval.Amount);
            return unitGroupCount;
        }
        
        public static DateTimeInterval SelectDateTimeUnitGroup(DateTimeInterval[] groups, DateTimeUnit unitContext, DateTimeUnit unitPrecision, DateTime startTime, DateTime endTime, double maxCount)
        {
            for (var i = groups.Length - 1; i >= 0; i--)
            {
                if (groups[i].TimeUnit > unitPrecision || groups[i].TimeUnit == unitPrecision && groups[i].Amount < 1)
                {
                    continue;
                }

                if (GetUnitGroupCount(groups[i], startTime, endTime) <= maxCount)
                {
                    return groups[i];
                }
            }

            return groups[0];
        }

        public static DateTime Add(this DateTime dateTime, DateTimeInterval interval)
        {
            var roundedAmount = (long)interval.Amount;
            dateTime = interval.TimeUnit switch
            {
                DateTimeUnit.Century => dateTime.AddYears((int)roundedAmount * 100),
                DateTimeUnit.Decade => dateTime.AddYears((int)roundedAmount * 10),
                DateTimeUnit.Year => dateTime.AddYears((int)roundedAmount),
                DateTimeUnit.Quarter => dateTime.AddMonths((int)roundedAmount * 3),
                DateTimeUnit.Month => dateTime.AddMonths((int)roundedAmount),
                DateTimeUnit.Day => dateTime.AddDays(roundedAmount),
                DateTimeUnit.Hour => dateTime.AddHours(roundedAmount),
                DateTimeUnit.Minute => dateTime.AddMinutes(roundedAmount),
                DateTimeUnit.Second => dateTime.AddSeconds(roundedAmount),
                _ => throw new ArgumentOutOfRangeException(nameof(interval.TimeUnit), interval.TimeUnit, null)
            };

            var fraction = interval.Amount - roundedAmount;
            
            dateTime = interval.TimeUnit switch
            {
                DateTimeUnit.Century => dateTime.AddMonths((int)Math.Floor(fraction * 12 * 100)),
                DateTimeUnit.Decade => dateTime.AddMonths((int)Math.Floor(fraction * 12 * 10)),
                DateTimeUnit.Year => dateTime.AddMonths((int)Math.Floor(fraction * 12)),
                DateTimeUnit.Quarter => dateTime.AddDays((int)Math.Floor(fraction * (dateTime.AddMonths(3) - dateTime).TotalDays)),
                DateTimeUnit.Month => dateTime.AddDays((int)Math.Floor(fraction * (dateTime.AddMonths(1) - dateTime).TotalDays)),
                DateTimeUnit.Day => dateTime.AddHours(Math.Floor(fraction * 24d)),
                DateTimeUnit.Hour => dateTime.AddMinutes(Math.Floor(fraction * 60)),
                DateTimeUnit.Minute => dateTime.AddSeconds(Math.Floor(fraction * 60)),
                DateTimeUnit.Second => dateTime.AddMilliseconds(Math.Floor(fraction * 1000)),
                _ => throw new ArgumentOutOfRangeException(nameof(interval.TimeUnit), interval.TimeUnit, null)
            };

            return dateTime;
        }

        public static DateTime Round(this DateTime time, DateTimeInterval interval, DateTime rootTime = default)
        {

            
            
            var intervalCount = interval.TimeUnit.GetDateTimeUnitCountBetween(rootTime, time, interval.Amount);

            var previousIntervalIndex = Math.Floor(intervalCount);

            var previous = rootTime.Add(new DateTimeInterval(interval.TimeUnit, previousIntervalIndex * interval.Amount));

            var next = rootTime.Add(new DateTimeInterval(interval.TimeUnit, (previousIntervalIndex + 1) * interval.Amount));

            return Math.Abs(time.Ticks - previous.Ticks)
                   < Math.Abs(next.Ticks - time.Ticks)
                ? previous
                : next;
        }

        public static DateTime RoundDown(this DateTime time, DateTimeUnit timeUnit)
        {
            var bla = time.Month / 3 * 3;
            return timeUnit switch
            {
                
                
                DateTimeUnit.Century => new DateTime((time.Year - 1) / 100 * 100 + 1, 1, 1, 0, 0, 0, time.Kind),
                DateTimeUnit.Decade => new DateTime((time.Year - 1) / 10 * 10 + 1, 1, 1, 0, 0, 0, time.Kind),
                DateTimeUnit.Year => new DateTime(time.Year, 1, 1, 0, 0, 0, time.Kind),
                DateTimeUnit.Quarter => new DateTime(time.Year, (time.Month - 1) / 3 * 3 + 1 , 1, 0, 0, 0, time.Kind),
                DateTimeUnit.Month => new DateTime(time.Year, time.Month, 1, 0, 0, 0, time.Kind),
                DateTimeUnit.Day => time.Date,
                DateTimeUnit.Hour => new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0, time.Kind),
                DateTimeUnit.Minute => new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, time.Kind),
                DateTimeUnit.Second => new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Kind),

                _ => throw new ArgumentOutOfRangeException(nameof(timeUnit))
            };
        }
        
        public static DateTime RoundDown(this DateTime time, DateTimeInterval interval, DateTime rootTime = default)
        {
            if (interval.Amount == 1 && rootTime == default)
            {
                return time.RoundDown(interval.TimeUnit);
            }
            
            var intervalCount = interval.TimeUnit.GetDateTimeUnitCountBetween(rootTime, time, interval.Amount);
            
            
            var previousIndex = Math.Floor(intervalCount);
            
            //Debug.Log($"previousIndex * interval.Amount = {previousIndex} * {interval.Amount} = {previousIndex * interval.Amount}");

            return rootTime.Add(new DateTimeInterval(
                interval.TimeUnit, previousIndex * interval.Amount));
        }
    }
}
