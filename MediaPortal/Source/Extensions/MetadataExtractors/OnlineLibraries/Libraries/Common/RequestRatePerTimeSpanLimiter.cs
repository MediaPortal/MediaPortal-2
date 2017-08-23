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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Extensions.OnlineLibraries.Libraries.Common
{
  /// <summary>
  /// Implementation of <see cref="IRequestRateLimiter"/> that enforces a
  /// request rate limit in the form of "maximum x requests per y time"
  /// </summary>
  public class RequestRatePerTimeSpanLimiter : IRequestRateLimiter, IDisposable
  {
    #region Private fields

    /// <summary>
    /// Used to limit concurrency to the maximum number of requests in a given TimeSpan
    /// </summary>
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// TimeSpan in which the maximum number of requests is accepted
    /// </summary>
    private readonly TimeSpan _perTimeSpan;

    /// <summary>
    /// Holds the time stamps at which the last numberOfRequests requests have been completed
    /// </summary>
    private readonly ConcurrentQueue<DateTime> _releaseTimes;

    /// <summary>
    /// Time at which a particular object of this class is created
    /// </summary>
    private readonly DateTime _startupTime;

    /// <summary>
    /// We need to use this Stopwatch (together with <see cref="_startupTime"/> to calculate
    /// the current time because <see cref="DateTime.Now"/> is not precise enough.
    /// </summary>
    private readonly Stopwatch _sw;

    #endregion

    #region Ctor

    /// <summary>
    /// Creates an instance of this class
    /// </summary>
    /// <param name="numberOfRequests">Number of requests that are accepted within <paramref name="perTimeSpan"/></param>
    /// <param name="perTimeSpan">TimeSpan within which <paramref name="numberOfRequests"/> requests are accepted</param>
    public RequestRatePerTimeSpanLimiter(int numberOfRequests, TimeSpan perTimeSpan)
    {
      _semaphore = new SemaphoreSlim(numberOfRequests, numberOfRequests);
      _perTimeSpan = perTimeSpan;
      _releaseTimes = new ConcurrentQueue<DateTime>();
      _startupTime = DateTime.UtcNow;
      _sw = Stopwatch.StartNew();

      // We initialize the release times with DateTime.MinValue to
      // symbolize that the last requests have been sent so long ago
      // that now numberOfRequests can be sent immediately without
      // infringing the request rate limit.
      for (var i = 1; i <= numberOfRequests; i++)
        _releaseTimes.Enqueue(DateTime.MinValue);
    }

    #endregion

    #region IRequestRateLimiter implementation

    /// <summary>
    /// Needs to be called before a new request is sent to obey the request rate limit
    /// </summary>
    /// <returns>Task that needs to be awaited before the next request can be sent</returns>
    public async Task RateLimit()
    {
      // If numberOfRequest requests are currently running, the request rate limit would
      // certainly be infringed if another one is sent now; in this case we already wait
      // here until a request has finished.
      await _semaphore.WaitAsync().ConfigureAwait(false);

      // We need to calculate the current time with Stopwatch because precision (in terms
      // of granularity) is more important to us than accuracy. DateTime.UtcNow has on some
      // systems a granularity of +/- 15ms or even more, whereas Stopwatch is below 1ms.
      var now = _startupTime + _sw.Elapsed;

      // In the _releaseTimes queue should always be numberOfRequests entries; the oldest
      // one of them is decisive for the question when the next request can be sent.
      DateTime oldestRelease;
      if (!_releaseTimes.TryDequeue(out oldestRelease))
      {
        ServiceRegistration.Get<ILogger>().Warn("RequestRatePerTimeSpanLimiter: _releaseTimes queue is empty. Some code has probably not called RequestDone()");
        oldestRelease = DateTime.MinValue;
      }
      var waitUntil = oldestRelease.Add(_perTimeSpan);
      if (waitUntil > now)
        await Task.Delay(waitUntil - now).ConfigureAwait(false);
    }

    /// <summary>
    /// Needs to be called after the request has reached the server
    /// </summary>
    /// <remarks>
    /// As we usually don't know, when a request reaches the server, this method should be
    /// called, when a response has been received as only this is a certain sign that the
    /// request has reached the server. Ideally, it should be called, when the headers of
    /// the response have been received so that we do not wait longer than necessary.
    /// </remarks>
    public void RequestDone()
    {
      _releaseTimes.Enqueue(_startupTime + _sw.Elapsed);
      _semaphore.Release();
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      _semaphore.Dispose();
    }

    #endregion
  }
}