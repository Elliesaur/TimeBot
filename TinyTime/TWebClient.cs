using System;
using System.Net;

namespace TinyTime
{
    public class TWebClient : WebClient
    {
        #region Public Properties

        public CookieContainer Cookies { get; set; }
        public int Timeout { get; set; }

        #endregion

        #region Fields

        public bool AllowAutoRedirect = true;
        public bool RemoveContent = false;

        #endregion

        #region Public Methods

        public void AddXHRHeaders(string referer, bool XHR = true)
        {

            Headers[HttpRequestHeader.Accept] = "application/json, text/javascript, */*; q=0.01";
            Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
            Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.8";
            Headers[HttpRequestHeader.Referer] = referer;
            Headers[HttpRequestHeader.UserAgent] =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3578.98 Safari/537.36";
            if (XHR)
                Headers["X-Requested-With"] = "XMLHttpRequest";

        }

        #endregion

        #region Protected Methods

        protected override WebRequest GetWebRequest(Uri address)
        {
            var wr = base.GetWebRequest(address) as HttpWebRequest;

            if (Cookies == null) Cookies = new CookieContainer();

            wr.Timeout = Timeout;
            wr.AllowAutoRedirect = AllowAutoRedirect;
            wr.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            wr.CookieContainer = Cookies;

            return wr;
        }

        #endregion
    }
}