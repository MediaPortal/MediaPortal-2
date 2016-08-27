#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Text;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using System.Net;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MusicBrainzV2
{
  /// <summary>
  /// Downloader that enforces the request rate limit on MusicBrainz
  /// </summary>
  /// <remarks>
  /// As per http://wiki.musicbrainz.org/XML_Web_Service/Rate_Limiting
  /// MusicBrainz enforces a request rate limit of 1 requests per second for an IP address.
  /// MusicBrainz enforces a request rate limit of 50 requests per second for a User-Agent,
  /// but is this per client?
  /// </remarks>
  internal class MusicBrainzDownloader : Downloader
  {
    private static readonly IRequestRateLimiter LIMITER = new RequestRatePerTimeSpanLimiter(10, TimeSpan.FromSeconds(1));

    private DateTime? _denyRequestsUntilTime = null;
    private const int REQUEST_DISABLE_TIME_IN_MINUTES = 10;
    private const int MAX_FAILED_REQUESTS = 3;

    public class RateLimitingException : Exception
    {
      public RateLimitingException(string message) : base(message)
      { }
    }

    public int RequestTimeouts { get; private set; }

    private void DisableRequestsTemporarily()
    {
      _denyRequestsUntilTime = DateTime.Now.AddMinutes(REQUEST_DISABLE_TIME_IN_MINUTES);
      RequestTimeouts = 0;
    }

    protected override string DownloadJSON(string url)
    {
      if (_denyRequestsUntilTime.HasValue)
      {
        if (_denyRequestsUntilTime.Value > DateTime.Now)
          throw new RateLimitingException("Requests disabled");
      }
      var webClient = new CompressionWebClient(EnableCompression) { Encoding = Encoding.UTF8 };
      foreach (var headerEntry in Headers)
        webClient.Headers[headerEntry.Key] = headerEntry.Value;

      LIMITER.RateLimit().Wait();
      try
      {
        return webClient.DownloadString(url);
      }
      catch (WebException ex)
      {
        if (ex.Response != null && ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.ServiceUnavailable)
        {
          //Rate limiting
          RequestTimeouts++;
          if (RequestTimeouts >= MAX_FAILED_REQUESTS)
          {
            DisableRequestsTemporarily();
          }
        }
        throw;
      }
      finally
      {
        LIMITER.RequestDone();
      }
    }
  }
}
