using System;
using System.Collections.Generic;
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

        /*
        public static List<DateTime> GetDateTimesBetween(this DateTimeUnit value, DateTime startTime, DateTime endTime, double timeUnitFactor = 1)
        {
            var dateTimes = new List<DateTime>();
            
            var anchor = value switch
            {
                DateTimeUnit.Year => new DateTime(1, 1, 1, 0, 0, 0, startTime.Kind),
                DateTimeUnit.Month => startTime.RoundDown(DateTimeUnit.Year),
                DateTimeUnit.Day => startTime.RoundDown(DateTimeUnit.Month),
                DateTimeUnit.Hour => startTime.RoundDown(DateTimeUnit.Day),
                DateTimeUnit.Minute => startTime.RoundDown(DateTimeUnit.Hour),
                DateTimeUnit.Second => startTime.RoundDown(DateTimeUnit.Minute),
                _ => throw new ArgumentOutOfRangeException(nameof(value))
            };

            for (var i = 0;; i++)
            {
                var time = anchor.Add(value, i * timeUnitFactor);
                if (time >= endTime)
                {
                    break;
                }

                if (time >= startTime && (dateTimes.Count == 0 || dateTimes[^1] != time))
                {
                    dateTimes.Add(time);
                }
            }

            return dateTimes;
        }*/
        
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
        
        /*
        Maybe slightly more optimized? Does the same thing, though.
        public static DateTimeUnitGroup SelectDateTimeUnitGroup(DateTimeUnitGroup[] groups, DateTime startTime, DateTime endTime, double maxCount, ref int groupIndex)
        {
            var currentIndex = groupIndex;
            
            while (true)
            {
                var currentCount = GetUnitGroupCount(groups[currentIndex], startTime, endTime);

                if (currentCount > maxCount)
                {
                    currentIndex--;
                    if (currentIndex == 0)
                    {
                        groupIndex = currentIndex;
                        return groups[groupIndex];
                    }
                    else
                    {
                        var nextCount = GetUnitGroupCount(groups[currentIndex + 1], startTime, endTime);
                        if (nextCount > maxCount)
                        {
                            groupIndex = currentIndex;
                            return groups[groupIndex];
                        }
                        else
                        {
                            currentIndex++;
                            if (currentIndex == groups.Length - 1)
                            {
                                groupIndex = currentIndex;
                                return groups[groupIndex];
                            }
                        }
                    }
                }
            }
        }*/
        
        
        public static DateTime Add(this DateTime dateTime, DateTimeInterval interval)
        {
            var roundedAmount = (int)interval.Amount;
            dateTime = interval.TimeUnit switch
            {
                DateTimeUnit.Year => dateTime.AddYears(roundedAmount),
                DateTimeUnit.Month => dateTime.AddMonths(roundedAmount),
                DateTimeUnit.Day => dateTime.AddDays(roundedAmount),
                DateTimeUnit.Hour => dateTime.AddHours(roundedAmount),
                DateTimeUnit.Minute => dateTime.AddMinutes(roundedAmount),
                DateTimeUnit.Second => dateTime.AddSeconds(roundedAmount),
                _ => throw new ArgumentOutOfRangeException(nameof(interval.TimeUnit), interval.TimeUnit, null)
            };

            var fraction = interval.Amount - roundedAmount;
            
            dateTime = interval.TimeUnit switch
            {
                DateTimeUnit.Year => dateTime.AddMonths((int)Math.Floor(fraction * 12)),
                DateTimeUnit.Month => dateTime.AddDays((int)Math.Floor(fraction * DateTime.DaysInMonth(dateTime.Year, dateTime.Month))),
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
        
        
        public static DateTime RoundDown(this DateTime time, DateTimeInterval interval, DateTime rootTime = default)
        {
            var intervalCount = interval.TimeUnit.GetDateTimeUnitCountBetween(rootTime, time, interval.Amount);
            
            var previousIndex = Math.Floor(intervalCount);

            return rootTime.Add(new DateTimeInterval(
                interval.TimeUnit, previousIndex * interval.Amount));
        }

        private static DateTime GetBaseTime(this DateTime time,  DateTimeUnit timeUnit, DateTime rootTime = default)
        {
            return timeUnit switch
            {
                DateTimeUnit.Year => new DateTime(1, rootTime.Month, rootTime.Day, rootTime.Hour, rootTime.Minute, rootTime.Second, time.Kind),
                DateTimeUnit.Month => new DateTime(time.Year, 1, rootTime.Day, rootTime.Hour, rootTime.Minute, rootTime.Second, time.Kind),
                DateTimeUnit.Day => new DateTime(time.Year, time.Month, 1, rootTime.Hour, rootTime.Minute, rootTime.Second, time.Kind),
                DateTimeUnit.Hour => new DateTime(time.Year, time.Month, time.Day, 0, rootTime.Minute, rootTime.Second, time.Kind),
                DateTimeUnit.Minute => new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, rootTime.Second, time.Kind),
                DateTimeUnit.Second => new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0, time.Kind),
                _ => throw new ArgumentOutOfRangeException(nameof(timeUnit), timeUnit, null)
            };
        }


        public static double GetUnitValue(this DateTime dateTime, DateTimeUnit timeUnit)
        {
            var unitValue = timeUnit switch
            {
                DateTimeUnit.Year => dateTime.Year - 1 + (dateTime.Month - 1) / 12d,
                DateTimeUnit.Month => dateTime.Month - 1 +
                                      (dateTime.Day - 1) / (double)DateTime.DaysInMonth(dateTime.Year, dateTime.Month),
                DateTimeUnit.Day => dateTime.Day - 1 + dateTime.Hour / 24d,
                DateTimeUnit.Hour => dateTime.Hour + dateTime.Minute / 60d,
                DateTimeUnit.Minute => dateTime.Minute + dateTime.Second / 60d,
                DateTimeUnit.Second => dateTime.Second + dateTime.Millisecond / 1000d,
                _ => throw new ArgumentOutOfRangeException(nameof(timeUnit),timeUnit, null)
            };
            return unitValue;
        }
    }
}
