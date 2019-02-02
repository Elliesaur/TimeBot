using System;
using System.Linq;
using NodaTime;
using NodaTime.Extensions;
using NodaTime.TimeZones;

namespace TinyTime
{
    public static class TinyTime
    {
        #region Public Methods

        /// <summary>
        ///     Convert a datetime to the specified timezone.
        /// </summary>
        /// <param name="utcDateTime">The date to convert.</param>
        /// <param name="timezoneDbEntry">The timezone DB entry, such as "Australia/Adelaide"</param>
        /// <returns>The converted datetime.</returns>
        public static ZonedDateTime ConvertToTime(DateTime utcDateTime, string timezoneDbEntry)
        {
            var convertToTimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(timezoneDbEntry);
            if (convertToTimeZone == null) throw new ArgumentException(nameof(timezoneDbEntry));
            var utcInstant = utcDateTime.ToInstant();
            var converted = utcInstant.InZone(convertToTimeZone);

            return converted;
        }

        /// <summary>
        ///     Find the time in a certain city or country, if city and country are specified both are used, if one is missing only
        ///     the other is used. If none are present it will return default(ZonedDateTime).
        /// </summary>
        /// <param name="city">The city, "New York"</param>
        /// <param name="country">The country, "United States"</param>
        /// <returns>The zoned date time of the current time in that location or default(ZonedDateTime).</returns>
        public static ZonedDateTime TimeIn(out TzdbZoneLocation loc, string city = null, string country = null)
        {
            IClock clock = SystemClock.Instance;

            TzdbZoneLocation location = null;

            if (!string.IsNullOrEmpty(country) && !string.IsNullOrEmpty(city))
                location = TzdbDateTimeZoneSource
                           .Default
                           .ZoneLocations
                           .FirstOrDefault(l => l.CountryName.ToLowerInvariant().StartsWith(country.ToLowerInvariant())
                                                && l.ZoneId.Split('/')[1]
                                                    .ToLowerInvariant()
                                                    .Replace("_", " ")
                                                    .StartsWith(city.ToLowerInvariant())
                           );
            else if (!string.IsNullOrEmpty(city))
                location = TzdbDateTimeZoneSource
                           .Default
                           .ZoneLocations
                           .FirstOrDefault(l => l.ZoneId.Split('/')[1]
                                                 .ToLowerInvariant()
                                                 .Replace("_", " ")
                                                 .StartsWith(city.ToLowerInvariant())
                           );
            else if (!string.IsNullOrEmpty(country))
                location = TzdbDateTimeZoneSource
                           .Default
                           .ZoneLocations
                           .FirstOrDefault(l => l.CountryName.ToLowerInvariant()
                                                 .StartsWith(country.ToLowerInvariant()));

            loc = location;
            if (location != null)
            {
                var zone = DateTimeZoneProviders.Tzdb[location.ZoneId];
                return clock.GetCurrentInstant().InZone(zone);
            }

            return default;
        }

        #endregion
    }
}