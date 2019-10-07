#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using MediaPortal.Utilities.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Matches
{
  /// <summary>
  /// Base class for online subtitle matchers.
  /// </summary>
  /// <typeparam name="TMatch">Type of match, must be derived from <see cref="BaseSubtitleMatch"/>.</typeparam>
  public abstract class BaseSubtitleMatcher<TId> : IDisposable
  {
    #region Fields

    /// <summary>
    /// Locking object to access settings.
    /// </summary>
    protected object _syncObj = new object();
    protected string _id;
    protected ApiSubtitleWrapper<TId> _wrapper;
    private bool _disposed;
    private bool _useHttps;

    #endregion

    #region Properties

    protected ILogger Logger
    {
      get
      {
        return ServiceRegistration.Get<ILogger>();
      }
    }

    public string Id
    {
      get { return _id; }
    }

    protected bool UseSecureWebCommunication
    {
      get
      {
        return _useHttps;
      }
    }

    #endregion

    protected BaseSubtitleMatcher()
    {
      OnlineLibrarySettings settings = ServiceRegistration.Get<ISettingsManager>().Load<OnlineLibrarySettings>();
      _useHttps = settings.UseSecureWebCommunication;
    }

    public virtual Task<bool> InitAsync()
    {
      return Task.FromResult(NetworkConnectionTracker.IsNetworkConnected);
    }

    public virtual async Task<bool> DownloadSubtitleAsync(Guid mediaItemId, SubtitleInfo info, bool replaceExisting)
    {
      if (info == null)
        return false;

      try
      {
        if (!await InitAsync().ConfigureAwait(false))
          return false;
        if (_wrapper == null)
          return false;

        string name = info.ToString();
        Logger.Debug(_id + " Download: Downloading subtitle for {0} [{1}]", info.MediaTitle, mediaItemId);

        var success = await _wrapper.DownloadSubtitleMatchesAsync(info, replaceExisting).ConfigureAwait(false);
        if (!success)
          return false;

        Logger.Debug(_id + " Download: Finished saving subtitles for {0} [{1}]", info, mediaItemId);
        return true;
      }
      catch (WebException)
      {
        //Remote server probably returned an error/not found, just log at debug level
        Logger.Debug(_id + " Download: WebException when downloading subtitles for {0} [{1}]", info.MediaTitle, mediaItemId);
      }
      catch (Exception ex)
      {
        Logger.Warn(_id + " Download: Failed downloading subtitles for {0} [{1}]", ex, info.MediaTitle, mediaItemId);
      }
      return false;
    }

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

      _disposed = true;
    }

    #endregion
  }
}
