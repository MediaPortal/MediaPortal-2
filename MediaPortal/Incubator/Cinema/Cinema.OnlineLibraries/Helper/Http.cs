using System;
using System.Net;
using HtmlAgilityPack;

namespace Cinema.OnlineLibraries.Helper
{

  internal class Http
  {
      /// <summary>
      ///     HttpStatusCode of the last Response
      /// </summary>
      public static System.Net.HttpStatusCode StatusCode = System.Net.HttpStatusCode.Unused;

      /// <summary>
      ///     Last Http Response
      /// </summary>
      public static string LastResponse { get; set; } = string.Empty;

      /// <summary>
      ///     Last called Url
      /// </summary>
      public static string Url { get; set; } = string.Empty;

      private static readonly string UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; WOW64; Trident/6.0;)";

      public static string Request(string url)
      {
        StatusCode = HttpStatusCode.Created;
        LastResponse = string.Empty;
        Url = url;

        try
        {
          if (!(System.Net.WebRequest.Create(url) is System.Net.HttpWebRequest request)) return LastResponse;
          request.UserAgent = UserAgent;
          request.Accept = "text/html, application/xhtml+xml, application/xml";
          request.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
          request.Timeout = 5000;

          using (System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse())
          {
            using (System.IO.Stream dataStream = response.GetResponseStream())
            {
              if (dataStream != null)
                using (System.IO.StreamReader reader = new System.IO.StreamReader(dataStream))
                {
                  StatusCode = response.StatusCode;
                  LastResponse = reader.ReadToEnd();
                }
            }
          }

          return LastResponse;
        }
        catch (Exception e)
        {
          Console.WriteLine(e);
        }

        return LastResponse;
      }

    public static HtmlDocument GetHtmlDocument(string url)
    {
      HtmlDocument web = new HtmlDocument();
      var ret = Http.Request(url);
      web.LoadHtml(ret);
      return web;
    }
  }
}
