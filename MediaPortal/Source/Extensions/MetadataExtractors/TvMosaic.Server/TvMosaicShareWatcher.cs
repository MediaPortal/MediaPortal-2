#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.SystemResolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TvMosaic.API;
using TvMosaicMetadataExtractor.ResourceAccess;

namespace TvMosaic.Server
{
  /// <summary>
  /// Class that periodically checks the available recorded tv items in TvMosaic and updates the recordings share if they have changed.
  /// </summary>
  public class TvMosaicShareWatcher : IDisposable
  {
    #region IEqualityComparer<RecordedTV>

    /// <summary>
    /// Equality comparer for <see cref="RecordedTV"/> that checks that both the <see cref="RecordedTV.ObjectID"/> and <see cref="RecordedTV.State"/> are equal.
    /// </summary>
    protected class RecordedTVComparer : IEqualityComparer<RecordedTV>
    {
      public bool Equals(RecordedTV x, RecordedTV y)
      {
        if (x == null)
          return y == null;
        if (y == null)
          return false;
        return x.ObjectID == y.ObjectID && x.State == y.State;
      }

      public int GetHashCode(RecordedTV obj)
      {
        if (obj == null)
          return 0;
        return obj.ObjectID.GetHashCode() ^ obj.State.GetHashCode();
      }
    }

    #endregion

    protected static readonly RecordedTVComparer _recordedTvComparer = new RecordedTVComparer();

    protected TimeSpan _initialDelay;
    protected TimeSpan _interval;
    protected CancellationTokenSource _cancellationTokenSource;
    protected Task _watcherTask;
    protected bool _started;
    protected DateTime? _previousUpdateTime;
    protected ICollection<RecordedTV> _previousAvailableRecordedTv;

    /// <summary>
    /// Starts polling for recorded tv changes at the specified interval. If previously started then this method just updates the interval for subsequent polls.  
    /// </summary>
    /// <param name="initialDelay">The initial time to wait before polling for recorded tv changes.</param>
    /// <param name="interval">The time to wait between polls for recorded tv changes.</param>
    public void Start(TimeSpan initialDelay, TimeSpan interval)
    {
      _initialDelay = initialDelay;
      _interval = interval;
      // If already started then just update the interval, it will be used for subsequent checks.
      if (_started)
        return;
      _cancellationTokenSource = new CancellationTokenSource();
      _watcherTask = WatchForRecordedTvChanges();
      _started = true;
    }

    /// <summary>
    /// Stops polling for recorded tv changes.
    /// </summary>
    /// <returns>Task that completes when polling has stopped.</returns>
    public async Task Stop()
    {
      if (!_started)
        return;
      _cancellationTokenSource.Cancel();
      await _watcherTask.ConfigureAwait(false);
      _cancellationTokenSource.Dispose();
      _started = false;
    }

    /// <summary>
    /// Watches for recorded tv changes by polling the TvMosaic server at the interval specified in <see cref="_interval"/>
    /// until cancellation is requested by <see cref="_cancellationTokenSource"/>. 
    /// </summary>
    /// <returns>Task that completes when polling is stopped.</returns>
    protected async Task WatchForRecordedTvChanges()
    {
      try
      {
        // Initial delay
        await Task.Delay(_initialDelay, _cancellationTokenSource.Token).ConfigureAwait(false);

        while (!_cancellationTokenSource.IsCancellationRequested)
        {
          // Refresh the share if required
          await RefreshRecordedTvShareIfRequired().ConfigureAwait(false);
          await Task.Delay(_interval, _cancellationTokenSource.Token).ConfigureAwait(false);
        }
      }
      catch (TaskCanceledException)
      {
        // Stop has been called
      }
    }

    /// <summary>
    /// Checks whether the recorded tv share needs refreshing and if so triggers a refresh of the share.
    /// </summary>
    /// <returns><see cref="Task"/> that completes when the check is complete and the refresh has started, if required.</returns>
    protected async Task RefreshRecordedTvShareIfRequired()
    {
      ICollection<RecordedTV> availableRecordedTv = await GetRecordedTvFromTvMosaic().ConfigureAwait(false);

      // If TvMosaic server is unreachable do nothing
      if (availableRecordedTv == null)
        return;

      // If TvMosaic recordings haven't changed since last check do nothing
      if (_previousAvailableRecordedTv != null && CollectionsContainSameElements(_previousAvailableRecordedTv, availableRecordedTv, _recordedTvComparer))
        return;
      _previousAvailableRecordedTv = availableRecordedTv;

      // The recordings have either changed or this is the first check, update the share.
      // For the first check we could check if the share is already up to date and avoid the initial refresh, but we'd need to
      // check that all items and their durations are up to date, as ongoing recordings might have changed duration since
      // their initial import, so to keep things simple just do a refresh, it will only occur once on server startup.
      Share recordedtvShare = GetRecordedTvShare();
      if (recordedtvShare != null)
        RefreshShare(recordedtvShare);
    }

    protected async Task<ICollection<RecordedTV>> GetRecordedTvFromTvMosaic()
    {
      // Create a new instance of the navigator every time to ensure that the latest settings are used
      TvMosaicNavigator navigator = new TvMosaicNavigator();
      return await navigator.GetChildItemsAsync(TvMosaicNavigator.RECORDED_TV_OBJECT_ID).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the share on the local system that is configured for TvMosaic recordings.
    /// </summary>
    /// <returns>If present, the local TvMosiac recordings <see cref="Share"/>; else <c>null</c>.</returns>
    protected Share GetRecordedTvShare()
    {
      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      IMediaLibrary mediaLibrary = ServiceRegistration.Get<IMediaLibrary>();
      var shares = mediaLibrary.GetShares(systemResolver.LocalSystemId);
      return shares.Values.FirstOrDefault(s => s.BaseResourcePath.BasePathSegment.ProviderId == TvMosaicResourceProvider.TVMOSAIC_RESOURCE_PROVIDER_ID);
    }

    /// <summary>
    /// Triggers a refresh of the specified share.
    /// </summary>
    protected void RefreshShare(Share share)
    {
      ServiceRegistration.Get<ILogger>().Debug($"{GetType().Name}: Refreshing TVMosaic recorded TV share");
      IImporterWorker importerWorker = ServiceRegistration.Get<IImporterWorker>();
      importerWorker.ScheduleRefresh(share.BaseResourcePath, share.MediaCategories, true);
    }

    /// <summary>
    /// Utility method to determine whether two collections contain the same elements, not necessarily in the same order.
    /// </summary>
    /// <typeparam name="T">The type contained in the collections.</typeparam>
    /// <param name="x">First collections to compare.</param>
    /// <param name="y">Second collection to compare.</param>
    /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to compare elements or <c>null</c> to use the default comparer.</param>
    /// <returns><c>true</c> if the collections contain the same elements; else <c>false</c>.</returns>
    protected static bool CollectionsContainSameElements<T>(ICollection<T> x, ICollection<T> y, IEqualityComparer<T> comparer = null)
    {
      if (x == null)
        return y == null;
      if (y == null)
        return false;
      return x.Count == y.Count && x.Intersect(y, comparer).Count() == x.Count;
    }

    public void Dispose()
    {
      Stop().Wait();
    }
  }
}
