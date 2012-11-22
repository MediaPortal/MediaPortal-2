#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Collections.Generic;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Utilities.Network;

namespace MediaPortal.Extensions.OnlineLibraries.Matches
{
  /// <summary>
  /// Base class for online matchers (Series, Movies) that provides common features like loading and saving match lists, download queue management.
  /// </summary>
  /// <typeparam name="TMatch">Type of match, must be derived from <see cref="BaseMatch{T}"/>.</typeparam>
  /// <typeparam name="TId">Type of internal ID of the match.</typeparam>
  public abstract class BaseMatcher<TMatch, TId> : IDisposable
    where TMatch : BaseMatch<TId>
  {
    #region Constants

    public const int MAX_FANART_IMAGES = 5;
    public const int MAX_FANART_DOWNLOADERS = 3;

    #endregion

    #region Fields

    protected abstract string MatchesSettingsFile { get; }

    /// <summary>
    /// Locking object to access settings.
    /// </summary>
    protected object _syncObj = new object();

    /// <summary>
    /// Contains the Series ID for Downloading FanArt asynchronously.
    /// </summary>
    protected UniqueEventedQueue<TId> _downloadQueue = new UniqueEventedQueue<TId>();
    protected List<Thread> _downloadThreads = new List<Thread>(MAX_FANART_DOWNLOADERS);
    protected bool _downloadAllowed = true;
    protected Predicate<TMatch> _matchPredicate;
    protected MatchStorage<TMatch, TId> _storage;

    #endregion

    protected BaseMatcher ()
    {
      // Use own thread to avoid delay during startup
      ServiceRegistration.Get<IThreadPool>().Add(ResumeDownloads, "ResumeDownloads", QueuePriority.Normal, ThreadPriority.BelowNormal);
    }

    protected virtual bool Init()
    {
      if (NetworkUtils.IsNetworkConnected)
        return false;
      if (_storage == null)
        _storage = new MatchStorage<TMatch, TId>(MatchesSettingsFile);
      return true;
    }

    public bool ScheduleDownload(TId tvDbId, bool force = false)
    {
      bool fanArtDownloaded = !force && CheckBeginDownloadFanArt(tvDbId);
      if (fanArtDownloaded)
        return true;

      lock (_downloadQueue.SyncObj)
      {
        bool newEnqueued = _downloadQueue.TryEnqueue(tvDbId);
        if (newEnqueued && _downloadThreads.Count < _downloadThreads.Capacity)
        {
          Thread downloader = new Thread(DownloadFanArtQueue) { Name = "FanArt Downloader " + _downloadThreads.Count, Priority = ThreadPriority.Lowest };
          downloader.Start();
          _downloadThreads.Add(downloader);
        }
      }
      return true;
    }

    public void EndDownloads()
    {
      lock (_downloadQueue.SyncObj)
      {
        _downloadQueue.Clear();
        _downloadAllowed = false;
      }
      foreach (Thread downloadThread in _downloadThreads)
        if (!downloadThread.Join(2000))
          downloadThread.Abort();

      _downloadThreads.Clear();
    }

    protected bool CheckBeginDownloadFanArt(TId itemId)
    {
      bool fanArtDownloaded = false;
      lock (_syncObj)
      {
        // Load cache or create new list
        List<TMatch> matches = _storage.LoadMatches();
        foreach (TMatch match in matches.FindAll(m => m.Id.Equals(itemId)))
        {
          // We can have multiple matches for one TvDbId in list, if one has FanArt downloaded already, update the flag for all matches.
          if (match.FanArtDownloadFinished.HasValue)
            fanArtDownloaded = true;

          if (!match.FanArtDownloadStarted.HasValue)
            match.FanArtDownloadStarted = DateTime.Now;
        }
        Settings.Save(MatchesSettingsFile, matches);
      }
      return fanArtDownloaded;
    }

    protected void FinishDownloadFanArt(TId itemId)
    {
      lock (_syncObj)
      {
        // Load cache or create new list
        List<TMatch> matches = _storage.LoadMatches();
        foreach (TMatch match in matches.FindAll(m => m.Id.Equals(itemId)))
          if (!match.FanArtDownloadFinished.HasValue)
            match.FanArtDownloadFinished = DateTime.Now;

        _storage.SaveMatches(matches);
      }
    }

    protected void ResumeDownloads()
    {
      if (!Init())
        return;

      List<TMatch> matches;
      lock (_syncObj)
        matches = _storage.LoadMatches();

      foreach (TMatch match in matches.FindAll(m => m.FanArtDownloadStarted.HasValue && !m.FanArtDownloadFinished.HasValue ||
          !m.Id.Equals(default(TId)) && !m.FanArtDownloadStarted.HasValue))
      {
        if (!match.FanArtDownloadStarted.HasValue)
          match.FanArtDownloadStarted = DateTime.Now;
        ScheduleDownload(match.Id, true);
      }
      Settings.Save(MatchesSettingsFile, matches);
    }

    protected void DownloadFanArtQueue()
    {
      while (_downloadAllowed)
      {
        _downloadQueue.OnEnqueued.WaitOne(1000);
        TId itemId;
        lock (_downloadQueue.SyncObj)
        {
          if (_downloadQueue.Count == 0)
            continue;
          itemId = _downloadQueue.Dequeue();
        }
        DownloadFanArt(itemId);
      }
    }

    protected abstract void DownloadFanArt(TId itemId);

    #region IDisposable members

    public void Dispose()
    {
      EndDownloads();
    }

    #endregion
  }
}