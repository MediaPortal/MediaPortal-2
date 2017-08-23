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
using System.Collections.Generic;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.Utilities.Network;

namespace MediaPortal.Extensions.OnlineLibraries.Matches
{
  /// <summary>
  /// Base class for online matchers (Series, Movies) that provides common features like loading and saving match lists, download queue management.
  /// </summary>
  /// <typeparam name="TMatch">Type of match, must be derived from <see cref="BaseFanArtMatch{T}"/>.</typeparam>
  /// <typeparam name="TId">Type of internal ID of the match.</typeparam>
  public abstract class BaseMatcher<TMatch, TId> : IDisposable
    where TMatch : BaseFanArtMatch<TId>
  {
    #region Constants

    public const int MAX_FANART_DOWNLOADERS = 3;
    public const string CONFIG_DATE_FORMAT = "MMddyyyyHHmm";

    #endregion

    #region Classes

    protected class FanartDownload<T>
    {
      public T Id { get; set; }
      public string DownloadId { get; set; }
    }

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
    protected UniqueEventedQueue<FanartDownload<TId>> _downloadQueue = new UniqueEventedQueue<FanartDownload<TId>>();
    protected List<Thread> _downloadThreads = new List<Thread>(MAX_FANART_DOWNLOADERS);
    protected bool _downloadFanart = true;
    protected volatile bool _downloadAllowed = true;
    protected Predicate<TMatch> _matchPredicate;
    protected MatchStorage<TMatch, TId> _storage;

    private bool _disposed;
    private bool _inited;
    private bool _useHttps;
    private bool _onlyBasicFanArt;

    #endregion

    #region Properties

    /// <summary>
    /// If set to <c>true</c> (default), online available content will be downloaded after match was successful.
    /// This property can be used to disable downloads, i.e. for testing process.
    /// </summary>
    public bool DownloadFanart
    {
      get { return _downloadFanart; }
      set { _downloadFanart = value; }
    }

    protected ILogger Logger
    {
      get
      {
        return ServiceRegistration.Get<ILogger>();
      }
    }

    protected bool UseSecureWebCommunication
    {
      get
      {
        return _useHttps;
      }
    }

    protected bool OnlyBasicFanArt
    {
      get
      {
        return _onlyBasicFanArt;
      }
    }

    #endregion

    protected BaseMatcher()
    {
      OnlineLibrarySettings settings = ServiceRegistration.Get<ISettingsManager>().Load<OnlineLibrarySettings>();
      _useHttps = settings.UseSecureWebCommunication;
      _onlyBasicFanArt = settings.OnlyBasicFanArt;
    }

    public virtual bool Init()
    {
      if (_storage == null)
        _storage = new MatchStorage<TMatch, TId>(MatchesSettingsFile);
      if (!_inited)
      {
        // Use own thread to avoid delay during startup
        IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>(false);
        if (threadPool != null)
          threadPool.Add(ResumeDownloads, "ResumeDownloads", QueuePriority.Normal, ThreadPriority.BelowNormal);
        _inited = true;
      }

      if (!NetworkConnectionTracker.IsNetworkConnected)
        return false;
      return true;
    }

    protected bool ScheduleDownload(TId id, string downloadId, bool force = false)
    {
      if (!_downloadFanart)
        return true;

      FanartDownload<TId> fanartDownload = new FanartDownload<TId> { Id = id, DownloadId = downloadId };
      //Always call CheckBeginDownloadFanart, even if we are forcing so the match storage is updated correctly
      bool fanArtDownloaded = CheckBeginDownloadFanArt(fanartDownload) && !force;
      if (fanArtDownloaded)
        return true;

      lock (_downloadQueue.SyncObj)
      {
        bool newEnqueued = _downloadQueue.TryEnqueue(fanartDownload);
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
        if (!downloadThread.Join(5000))
          downloadThread.Abort();

      _downloadThreads.Clear();
    }

    protected bool CheckBeginDownloadFanArt(FanartDownload<TId> fanartDownload)
    {
      bool fanArtDownloaded = false;
      lock (_syncObj)
      {
        // Load cache or create new list
        List<TMatch> matches = _storage.GetMatches();
        foreach (TMatch match in matches.FindAll(m => m.Id != null && m.Id.Equals(fanartDownload.Id)))
        {
          if (string.IsNullOrEmpty(match.FanArtDownloadId))
            match.FanArtDownloadId = fanartDownload.DownloadId;

          // We can have multiple matches for one TvDbId in list, if one has FanArt downloaded already, update the flag for all matches.
          if (match.FanArtDownloadFinished.HasValue)
            fanArtDownloaded = true;

          if (!match.FanArtDownloadStarted.HasValue)
            match.FanArtDownloadStarted = DateTime.Now;
        }

        //It's possible that we have multiple matches with the same id, ensure that they are all marked as finished
        if (fanArtDownloaded)
          foreach (TMatch match in matches.FindAll(m => m.Id != null && m.Id.Equals(fanartDownload.Id) && !m.FanArtDownloadFinished.HasValue))
            match.FanArtDownloadFinished = DateTime.Now;

        _storage.SaveMatches();
      }
      return fanArtDownloaded;
    }

    protected void FinishDownloadFanArt(FanartDownload<TId> fanartDownload)
    {
      lock (_syncObj)
      {
        // Load cache or create new list
        List<TMatch> matches = _storage.GetMatches();
        foreach (TMatch match in matches.FindAll(m => m.Id != null && m.Id.Equals(fanartDownload.Id)))
          if (!match.FanArtDownloadFinished.HasValue)
            match.FanArtDownloadFinished = DateTime.Now;

        _storage.SaveMatches();
      }
    }

    protected void ResumeDownloads()
    {
      var downloadsToBeStarted = new HashSet<TMatch>();
      lock (_syncObj)
      {
        var matches = _storage.GetMatches();
        foreach (TMatch match in matches.FindAll(m => m.FanArtDownloadStarted.HasValue && !m.FanArtDownloadFinished.HasValue ||
                                                      m.Id != null && !m.Id.Equals(default(TId)) && !m.FanArtDownloadStarted.HasValue))
        {
          if (string.IsNullOrEmpty(match.FanArtDownloadId))
          {
            match.FanArtDownloadFinished = DateTime.Now;
            continue;
          }
          if (!match.FanArtDownloadStarted.HasValue)
            match.FanArtDownloadStarted = DateTime.Now;
          downloadsToBeStarted.Add(match);
        }
        _storage.SaveMatches();
      }
      foreach (var match in downloadsToBeStarted)
        ScheduleDownload(match.Id, match.FanArtDownloadId, true);
    }

    protected void DownloadFanArtQueue()
    {
      while (_downloadAllowed)
      {
        _downloadQueue.OnEnqueued.WaitOne(1000);
        FanartDownload<TId> fanartDownload;
        lock (_downloadQueue.SyncObj)
        {
          if (_downloadQueue.Count == 0)
            continue;
          fanartDownload = _downloadQueue.Dequeue();
        }
        DownloadFanArt(fanartDownload);
      }
    }

    protected abstract void DownloadFanArt(FanartDownload<TId> fanartDownload);

    #region IDisposable members

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (_disposed)
        return;
      if (disposing)
      {
        EndDownloads();
        if (_storage != null)
          _storage.Dispose();
      }
      _disposed = true;
    }

    #endregion
  }
}
