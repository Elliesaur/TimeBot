using System;
using System.Collections.Generic;
using System.Net;
using NodaTime;

namespace TinyTime
{
    public class TimeIsAPI
    {
        #region Fields

        private static readonly Cache<string, string> _cachedResponses = new Cache<string, string>();

        #endregion

        #region Public Methods

        public static IEnumerable<SearchResult> Search(string query)
        {
            string cachedResponse = _cachedResponses.Get(query.ToLower());
            if (cachedResponse != default) return SearchResult.FromSearch(cachedResponse);

            string url = GetUrl(query);
            if (url == null) return new List<SearchResult>();

            using (var wc = new TWebClient
            {
                Timeout = 5000
            })
            {

                wc.AddXHRHeaders("https://time.is/");
                try
                {
                    string data = wc.DownloadString(url);
                    _cachedResponses.Store(query.ToLower(), data, TimeSpan.FromHours(3));
                    return SearchResult.FromSearch(data);
                }
                catch (Exception)
                {
                    return new List<SearchResult>();
                }
            }
        }

        #endregion

        #region Private Methods

        private static string GetUrl(string query)
        {
            query = WebUtility.UrlEncode(query.Replace(" ", "_"));
            if (query == null) return null;
            return $"https://time.is/s/en/{query.Length}/{query}?{UnixMS()}";
        }

        private static long UnixMS()
        {
            return SystemClock.Instance.GetCurrentInstant().ToUnixTimeMilliseconds();
        }

        #endregion

        #region Nested type: SearchResult

        public class SearchResult
        {
            #region Public Properties

            public string FriendlyName => Raw[0];
            public int OffsetMinutes => int.Parse(Raw[4]);
            public string TimeZoneIdentifier => Raw[10];
            public string Latitude => Raw[12];
            public string Longitude => Raw[13];

            public string[] Raw { get; }

            #endregion

            #region Constructors

            private SearchResult(string[] data)
            {
                Raw = data;
            }

            #endregion

            #region Public Methods

            public ZonedDateTime GetZonedDateTime()
            {
                return SystemClock.Instance.GetCurrentInstant().InZone(GetTimeZone());
            }

            public DateTimeZone GetTimeZone()
            {
                return DateTimeZone.ForOffset(Offset.FromSeconds(OffsetMinutes * 60));
            }

            public static IEnumerable<SearchResult> FromSearch(string webResponse)
            {
                string[] newLineData = webResponse.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string result in newLineData)
                {
                    if (!result.Contains("$")) continue;
                    string[] raw = result.Split('\t');
                    yield return new SearchResult(raw);
                }
            }

            #endregion
        }

        #endregion
    }
}