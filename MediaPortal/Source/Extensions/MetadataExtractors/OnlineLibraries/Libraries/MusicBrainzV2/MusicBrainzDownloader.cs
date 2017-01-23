#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Collections.Generic;

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
    private static DateTime? _denyRequestsUntilTime = null;
    private const int REQUEST_DISABLE_TIME_IN_MINUTES = 1;
    private const int MAX_FAILED_REQUESTS = 5;
    private static int _currentMirror = 0;
    private static int _failedRequests = 0;
    private static object _requestSync = new object();

    public MusicBrainzDownloader() : base()
    {
      Mirrors = new List<string>();
    }

    public List<string> Mirrors { get; private set; }

    public bool RequestsDisabled
    {
      get
      {
        if (_denyRequestsUntilTime.HasValue && _denyRequestsUntilTime.Value > DateTime.Now)
          return true;
        return false;
      }
    }

    private void DisableRequestsTemporarily()
    {
      lock (_requestSync)
      {
        _denyRequestsUntilTime = DateTime.Now.AddMinutes(REQUEST_DISABLE_TIME_IN_MINUTES);
      }
    }

    protected override string DownloadJSON(string url)
    {
      bool retry = false;
      lock (_requestSync)
      {
        if (_denyRequestsUntilTime.HasValue)
        {
          if (RequestsDisabled)
            return null;
          _failedRequests = 0;
          _denyRequestsUntilTime = null;
        }
      }
      var webClient = new CompressionWebClient(EnableCompression) { Encoding = Encoding.UTF8 };
      foreach (var headerEntry in Headers)
        webClient.Headers[headerEntry.Key] = headerEntry.Value;

      LIMITER.RateLimit().Wait();
      try
      {
        string fullUrl = Mirrors[_currentMirror] + url;
        string json = webClient.DownloadString(fullUrl);
        if (_failedRequests > 0)
          _failedRequests--;
        return json;
      }
      catch (WebException ex)
      {
        if (ex.Status == WebExceptionStatus.Timeout ||
          (ex.Response != null &&
          (
            (((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.ServiceUnavailable ||
            ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.RequestTimeout)
          )))
        {
          //Rate limiting
          _failedRequests++;
          if (_failedRequests >= MAX_FAILED_REQUESTS)
          {
            DisableRequestsTemporarily();
            return null;
          }
          _currentMirror = (_currentMirror + 1) % Mirrors.Count;
          retry = true;
        }
        else
        {
          throw;
        }
      }
      finally
      {
        LIMITER.RequestDone();
      }

      if(retry)
        return DownloadJSON(url);
      return null;
    }
  }
}
