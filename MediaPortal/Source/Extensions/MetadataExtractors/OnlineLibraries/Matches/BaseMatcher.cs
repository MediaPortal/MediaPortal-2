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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using MediaPortal.Utilities.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Matches
{
  /// <summary>
  /// Base class for online matchers (Series, Movies) that provides common features like loading and saving match lists.
  /// </summary>
  /// <typeparam name="TMatch">Type of match, must be derived from <see cref="BaseFanArtMatch{T}"/>.</typeparam>
  /// <typeparam name="TId">Type of internal ID of the match.</typeparam>
  public abstract class BaseMatcher<TMatch, TId, TImg, TLang> : IDisposable
    where TMatch : BaseMatch<TId>
  {
    #region Constants
    
    public const string CONFIG_DATE_FORMAT = "MMddyyyyHHmm";
    public static string FANART_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\FanArt\");

    #endregion

    #region Fields

    protected abstract string MatchesSettingsFile { get; }

    /// <summary>
    /// Locking object to access settings.
    /// </summary>
    protected object _syncObj = new object();
    
    protected bool _downloadFanart = true;
    protected Predicate<TMatch> _matchPredicate;
    protected MatchStorage<TMatch, TId> _storage;

    protected string _id;
    protected ApiWrapper<TImg, TLang> _wrapper;
    private bool _disposed;
    private bool _useHttps;
    private bool _onlyBasicFanArt;

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

    public virtual Task<bool> InitAsync()
    {
      if (_storage == null)
        _storage = new MatchStorage<TMatch, TId>(MatchesSettingsFile);
      return Task.FromResult(NetworkConnectionTracker.IsNetworkConnected);
    }

    protected virtual bool TryGetFanArtInfo(BaseInfo info, out TLang language, out string fanArtMediaType, out bool includeThumbnails)
    {
      language = default(TLang);
      fanArtMediaType = null;
      includeThumbnails = false;
      return false;
    }

    public virtual async Task<bool> DownloadFanArtAsync(Guid mediaItemId, BaseInfo info, bool force)
    {
      if (info == null)
        return false;

      try
      {
        if (!await InitAsync().ConfigureAwait(false))
          return false;
        if (_wrapper == null)
          return false;

        TLang language;
        string fanArtMediaType;
        bool includeThumbnails;
        if (!TryGetFanArtInfo(info, out language, out fanArtMediaType, out includeThumbnails))
          return false;

        ApiWrapperImageCollection<TImg> images = await _wrapper.GetFanArtAsync(info, language, fanArtMediaType).ConfigureAwait(false);
        if (images == null)
          return false;

        string name = info.ToString();
        string mediaItem = mediaItemId.ToString().ToUpperInvariant();
        Logger.Debug(_id + " Download: Downloading images for {0} [{1}]", info, mediaItemId);
        SaveFanArtImages(images.Id, images.Backdrops, language, mediaItem, name, FanArtTypes.FanArt);
        SaveFanArtImages(images.Id, images.Posters, language, mediaItem, name, FanArtTypes.Poster);
        SaveFanArtImages(images.Id, images.Banners, language, mediaItem, name, FanArtTypes.Banner);
        SaveFanArtImages(images.Id, images.Covers, language, mediaItem, name, FanArtTypes.Cover);
        if (includeThumbnails)
          SaveFanArtImages(images.Id, images.Thumbnails, language, mediaItem, name, FanArtTypes.Thumbnail);
        if (!OnlyBasicFanArt)
        {
          SaveFanArtImages(images.Id, images.ClearArt, language, mediaItem, name, FanArtTypes.ClearArt);
          SaveFanArtImages(images.Id, images.DiscArt, language, mediaItem, name, FanArtTypes.DiscArt);
          SaveFanArtImages(images.Id, images.Logos, language, mediaItem, name, FanArtTypes.Logo);
        }
        Logger.Debug(_id + " Download: Finished saving images for {0} [{1}]", info, mediaItemId);
        return true;
      }
      catch (Exception ex)
      {
        Logger.Warn(_id + " Download: Failed downloading images for {0} [{1}]", ex, info, mediaItemId);
      }
      return false;
    }

    protected virtual bool VerifyFanArtImage(TImg image, TLang language)
    {
      return image != null;
    }

    protected virtual int SaveFanArtImages(string id, IEnumerable<TImg> images, TLang language, string mediaItemId, string name, string fanartType)
    {
      try
      {
        if (images == null)
          return 0;

        int idx = 0;
        foreach (TImg img in images)
        {
          using (FanArtCache.FanArtCountLock countLock = FanArtCache.GetFanArtCountLock(mediaItemId, fanartType))
          {
            if (countLock.Count >= FanArtCache.MAX_FANART_IMAGES[fanartType])
              break;
            if (!VerifyFanArtImage(img, language))
              continue;
            if (idx >= FanArtCache.MAX_FANART_IMAGES[fanartType])
              break;
            FanArtCache.InitFanArtCache(mediaItemId, name);
            if (_wrapper.DownloadFanArt(id, img, Path.Combine(FANART_CACHE_PATH, mediaItemId, fanartType)))
            {
              countLock.Count++;
              idx++;
            }
            else
            {
              Logger.Warn(_id + " Download: Error downloading FanArt for ID {0} on media item {1} ({2}) of type {3}", id, mediaItemId, name, fanartType);
            }
          }
        }
        Logger.Debug(_id + @" Download: Saved {0} for media item {1} ({2}) of type {3}", idx, mediaItemId, name, fanartType);
        return idx;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + " Download: Exception downloading images for ID {0} [{1} ({2})]", ex, id, mediaItemId, name);
        return 0;
      }
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
      if (disposing)
      {
        if (_storage != null)
          _storage.Dispose();
      }
      _disposed = true;
    }

    #endregion
  }
}
