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
                DateTimeUnit.Year => GetYears(),
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

            var anchor = interval.Amount < 1 ? startTime.RoundDown(interval.TimeUnit) : startTime.RoundDown(interval);

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
            if (interval.TimeUnit is DateTimeUnit.Year or DateTimeUnit.Month)
            {
                var wholeAmount = (int)Math.Floor(interval.Amount);
                var fraction = interval.Amount - wholeAmount;
                var previous = (interval.TimeUnit == DateTimeUnit.Year)
                    ? dateTime.AddYears(wholeAmount)
                    : dateTime.AddMonths(wholeAmount);
                var next = (interval.TimeUnit == DateTimeUnit.Year)
                    ? dateTime.AddYears(wholeAmount + 1)
                    : dateTime.AddMonths(wholeAmount + 1);
                var ticks = (long)Math.Round((next.Ticks - previous.Ticks) * fraction);
                return previous.AddTicks(ticks);
            }
            
            var ticksPerUnit = interval.TimeUnit switch
            {
                DateTimeUnit.Day => TimeSpan.TicksPerDay,
                DateTimeUnit.Hour => TimeSpan.TicksPerHour,
                DateTimeUnit.Minute => TimeSpan.TicksPerMinute,
                DateTimeUnit.Second => TimeSpan.TicksPerSecond,
                DateTimeUnit.Year => throw new ArgumentOutOfRangeException(nameof(interval.TimeUnit)),
                DateTimeUnit.Month => throw new ArgumentOutOfRangeException(nameof(interval.TimeUnit)),
                _ => throw new ArgumentOutOfRangeException(nameof(interval.TimeUnit))
            };

            return dateTime.AddTicks(
                checked((long)Math.Round(interval.Amount * ticksPerUnit)));
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
            return timeUnit switch
            {
                DateTimeUnit.Year => new DateTime(time.Year, 1, 1, 0, 0, 0, time.Kind),
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
