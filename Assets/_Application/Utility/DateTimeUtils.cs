using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        
        public static double GetDateTimeUnitCountBetween(this DateTimeUnit timeUnit, DateTime startTime, DateTime endTime, double timeUnitFactor = 1)
        {
            if (startTime == endTime) return 0d;
                
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

                var previous = startTime.AddYearsClamped(count);
                var next = startTime.AddYearsClamped(count + 1);

                return count + (endTime - previous).TotalDays / (next - previous).TotalDays;
            }
            
            double GetMonths()
            {
                var count = (endTime.Year - startTime.Year) * 12 + endTime.Month - startTime.Month;
                if (startTime.AddMonths(count) > endTime) count--;

                var previous = startTime.AddMonthsClamped(count);
                var next = startTime.AddMonthsClamped(count + 1);

                return count + (endTime - previous).TotalDays / (next - previous).TotalDays;
            }
        }

        public static List<DateTime> GetDateTimesBetween(DateTime startTime, DateTime endTime, DateTimeInterval interval, bool alignGroupedIntervals, DateTime rootTime = default)
        {
            
            var results = new List<DateTime>();
            GetDateTimesBetweenNonAlloc(results, startTime, endTime, interval, alignGroupedIntervals, rootTime);
            
            return results;
        }

        public static void GetDateTimesBetweenNonAlloc(List<DateTime> results, DateTime startTime, DateTime endTime, DateTimeInterval interval, bool alignGroupedIntervals, DateTime rootTime = default)
        {
            results.Clear();
            if (endTime <= startTime) return;

            var anchor = GetRoundingRoot(startTime, interval, alignGroupedIntervals, rootTime);
            var previousIndex = GetPreviousIntervalIndex(startTime, interval, anchor);

            for (var index = previousIndex; ; index++)
            {
                var time = GetIntervalTime(anchor, interval, index);
                if (time >= endTime)
                {
                    break;
                }

                if (time >= startTime && (results.Count == 0 || results[^1] != time))
                {
                    results.Add(time);
                }
            }
            
            Debug.Log($"GetDateTimesBetween {startTime} {endTime} {interval} {results.Count}");
            
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
                    ? dateTime.AddYearsClamped(wholeAmount)
                    : dateTime.AddMonthsClamped(wholeAmount);
                if (fraction == 0) return previous;

                var next = (interval.TimeUnit == DateTimeUnit.Year)
                    ? dateTime.AddYearsClamped(wholeAmount + 1)
                    : dateTime.AddMonthsClamped(wholeAmount + 1);
                var ticks = (long)Math.Round((next.Ticks - previous.Ticks) * fraction);
                return previous.AddTicksClamped(ticks);
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
        
        

        public static DateTime Round(this DateTime time, DateTimeInterval interval, bool alignGroupedIntervals, DateTime rootTime = default)
        {
            var (previous, next) = GetIntervalBounds(time, interval, alignGroupedIntervals, rootTime);

            return Math.Abs(time.Ticks - previous.Ticks)
                   < Math.Abs(next.Ticks - time.Ticks)
                ? previous
                : next;
        }
        
        public static DateTime RoundDown(this DateTime time, DateTimeInterval interval, bool alignGroupedIntervals, DateTime rootTime = default)
        {
            if (interval.Amount == 1 && rootTime == default)
            {
                return time.RoundDown(interval.TimeUnit);
            }
            
            var anchor = GetRoundingRoot(time, interval, alignGroupedIntervals, rootTime);
            var index = GetPreviousIntervalIndex(time, interval, anchor);
            return GetIntervalTime(anchor, interval, index);
        }
        
        public static DateTime RoundDown(this DateTime time, DateTimeUnit timeUnit, DateTime rootTime = default)
        {
            return timeUnit switch
            {
                DateTimeUnit.Year => new DateTime(time.Year, rootTime.Month, rootTime.Day, rootTime.Hour, rootTime.Minute, rootTime.Second, time.Kind),
                DateTimeUnit.Month => new DateTime(time.Year, time.Month, rootTime.Day, rootTime.Hour, rootTime.Minute, rootTime.Second, time.Kind),
                DateTimeUnit.Day => new DateTime(time.Year, time.Month, time.Day, rootTime.Hour, rootTime.Minute, rootTime.Second, time.Kind),
                DateTimeUnit.Hour => new DateTime(time.Year, time.Month, time.Day, time.Hour, rootTime.Minute, rootTime.Second, time.Kind),
                DateTimeUnit.Minute => new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, rootTime.Second, time.Kind),
                DateTimeUnit.Second => new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Kind),

                _ => throw new ArgumentOutOfRangeException(nameof(timeUnit))
            };
        }

        private static DateTime GetRoundingRoot(DateTime time, DateTimeInterval interval, bool alignGroupedIntervals, DateTime rootTime)
        {
            if (interval.Amount < 1)
            {
                var lowerTicks = interval.TimeUnit switch
                {
                    DateTimeUnit.Year => (rootTime.Day - 1) * TimeSpan.TicksPerDay + rootTime.TimeOfDay.Ticks,
                    DateTimeUnit.Month => rootTime.TimeOfDay.Ticks,
                    DateTimeUnit.Day => rootTime.Ticks % TimeSpan.TicksPerHour,
                    DateTimeUnit.Hour => rootTime.Ticks % TimeSpan.TicksPerMinute,
                    DateTimeUnit.Minute => rootTime.Ticks % TimeSpan.TicksPerSecond,
                    DateTimeUnit.Second => rootTime.Ticks % TimeSpan.TicksPerMillisecond,
                    _ => throw new ArgumentOutOfRangeException(nameof(interval.TimeUnit))
                };

                return time.RoundDown(interval.TimeUnit).AddTicks(lowerTicks);
            }

            if (!alignGroupedIntervals || interval.Amount <= 1) return rootTime;
            
            

            var offset = interval.TimeUnit switch
            {
                DateTimeUnit.Year => 0,
                DateTimeUnit.Month => rootTime.Month - 1,
                DateTimeUnit.Day => rootTime.Day - 1,
                DateTimeUnit.Hour => rootTime.Hour,
                DateTimeUnit.Minute => rootTime.Minute,
                DateTimeUnit.Second => rootTime.Second,
                _ => throw new ArgumentOutOfRangeException(nameof(interval.TimeUnit))
            };

            return offset == 0
                ? rootTime
                : rootTime.Add(new DateTimeInterval(interval.TimeUnit, -offset));
        }

        private static DateTime GetIntervalTime(DateTime anchor, DateTimeInterval interval, double index)
        {
            if (interval.Amount >= 1)
            {
                return anchor.Add(new DateTimeInterval(interval.TimeUnit, index * interval.Amount));
            }

            var subdivisions = 1d / interval.Amount;
            var wholeUnits = Math.Floor(index / subdivisions);
            var indexInUnit = index - wholeUnits * subdivisions;
            var unitStart = anchor.Add(new DateTimeInterval(interval.TimeUnit, wholeUnits));

            // Convert each cumulative fraction to whole units of the next smaller scale.
            // Keeping the index avoids repeatedly adding a rounded interval.
            return interval.TimeUnit switch
            {
                DateTimeUnit.Year => unitStart.AddMonths((int)Math.Floor(indexInUnit * 12 / subdivisions)),
                DateTimeUnit.Month => unitStart.AddDays(Math.Floor(indexInUnit * DateTime.DaysInMonth(unitStart.Year, unitStart.Month) / subdivisions)),
                DateTimeUnit.Day => unitStart.AddHours(Math.Floor(indexInUnit * 24 / subdivisions)),
                DateTimeUnit.Hour => unitStart.AddMinutes(Math.Floor(indexInUnit * 60 / subdivisions)),
                DateTimeUnit.Minute => unitStart.AddSeconds(Math.Floor(indexInUnit * 60 / subdivisions)),
                DateTimeUnit.Second => unitStart.AddTicks((long)Math.Floor(indexInUnit * 1000 / subdivisions) * TimeSpan.TicksPerMillisecond),
                _ => throw new ArgumentOutOfRangeException(nameof(interval.TimeUnit))
            };
        }

        private static double GetPreviousIntervalIndex(DateTime time, DateTimeInterval interval, DateTime anchor)
        {
            if (interval.Amount <= 0 || double.IsNaN(interval.Amount) || double.IsInfinity(interval.Amount))
            {
                throw new ArgumentOutOfRangeException(nameof(interval));
            }

            var count = interval.TimeUnit.GetDateTimeUnitCountBetween(anchor, time, interval.Amount);
            var index = Math.Floor(count);

            // The count is an estimate: subdivision boundaries use whole smaller units.
            while (GetIntervalTime(anchor, interval, index) > time)
            {
                index--;
            }

            while (true)
            {
                var next = GetIntervalTime(anchor, interval, index + 1);

                if (next > time || next == DateTime.MaxValue)
                    break;

                index++;
            }

            return index;
        }

        private static (DateTime Previous, DateTime Next) GetIntervalBounds(DateTime time, DateTimeInterval interval, bool alignGroupedIntervals, DateTime rootTime)
        {
            var anchor = GetRoundingRoot(time, interval, alignGroupedIntervals, rootTime);
            var index = GetPreviousIntervalIndex(time, interval, anchor);
            return (GetIntervalTime(anchor, interval, index), GetIntervalTime(anchor, interval, index + 1));
        }
        
        /// <summary>
        /// Add value and clamp the returned result between DateTime.MinValue and DateTime.MaxValue
        /// </summary>
        public static DateTime AddClamped(this DateTime time, TimeSpan value)
        {
            return time.AddTicksClamped(value.Ticks);
        }

        public static DateTime AddYearsClamped(this DateTime time, int years)
        {
            var targetYear = (long)(time.Year - 1) + years;
            return targetYear switch
            {
                < 0 => new DateTime(DateTime.MinValue.Ticks, time.Kind),
                >= 9999L => new DateTime(DateTime.MaxValue.Ticks, time.Kind),
                _ => time.AddYears(years)
            };
        }

        public static DateTime AddMonthsClamped(this DateTime time, int months)
        {
            var targetMonth = (time.Year - 1) * 12L + (time.Month - 1) + months;
            return targetMonth switch
            {
                < 0 => new DateTime(DateTime.MinValue.Ticks, time.Kind),
                >= 9999L * 12 => new DateTime(DateTime.MaxValue.Ticks, time.Kind),
                _ => time.AddMonths(months)
            };
        }

        public static DateTime AddTicksClamped(this DateTime time, long value)
        {
            var ticks = time.Ticks;

            if (value > DateTime.MaxValue.Ticks - ticks)
            {
                ticks = DateTime.MaxValue.Ticks;
            }
            else if (value < -ticks)
            {
                ticks = DateTime.MinValue.Ticks;
            }
            else
            {
                ticks += value;
            }

            return new DateTime(ticks, time.Kind);
        }

    }
}
