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

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3
{
  /// <summary>
  /// Downloader that enforces the request rate limit on www.themoviedb.org
  /// </summary>
  /// <remarks>
  /// As per http://docs.themoviedb.apiary.io/#introduction/request-rate-limiting
  /// www.themoviedb.org enforces a request rate limit of 40 requests per 10 seconds.
  /// As per https://www.themoviedb.org/talk/52f3f24ac3a36801cd272f45
  /// this request rate limit is only enforced on API calls. On the download of image
  /// files from  http://image.tmdb.org/t/p/ there is no request rate limit.
  /// </remarks>
  internal class MovieDbDownloader : Downloader
  {
    private static readonly IRequestRateLimiter LIMITER = new RequestRatePerTimeSpanLimiter(40, TimeSpan.FromSeconds(10));

    protected override string DownloadJSON(string url)
    {
      var webClient = new CompressionWebClient(EnableCompression) { Encoding = Encoding.UTF8 };
      foreach (var headerEntry in Headers)
        webClient.Headers[headerEntry.Key] = headerEntry.Value;

      LIMITER.RateLimit().Wait();
      try
      {
        return webClient.DownloadString(url);
      }
      finally
      {
        LIMITER.RequestDone();
      }
    }
  }
}
