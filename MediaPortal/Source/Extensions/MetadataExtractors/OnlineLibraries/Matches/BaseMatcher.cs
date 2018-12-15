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
  /// Base class for online matchers (Series, Movies) that provides common features like loading and saving match lists.
  /// </summary>
  /// <typeparam name="TMatch">Type of match, must be derived from <see cref="BaseFanArtMatch{T}"/>.</typeparam>
  /// <typeparam name="TId">Type of internal ID of the match.</typeparam>
  public abstract class BaseMatcher<TMatch, TId, TImg, TLang> : IDisposable
    where TMatch : BaseMatch<TId>
  {
    #region Constants

    public const string CONFIG_DATE_FORMAT = "MMddyyyyHHmm";

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

    public virtual async Task<bool> DownloadFanArtAsync(Guid mediaItemId, BaseInfo info)
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
        Logger.Debug(_id + " Download: Downloading images for {0} [{1}]", info, mediaItemId);

        await SaveFanArtImagesAsync(images.Id, images.Backdrops, language, mediaItemId, name, FanArtTypes.FanArt).ConfigureAwait(false);
        await SaveFanArtImagesAsync(images.Id, images.Posters, language, mediaItemId, name, FanArtTypes.Poster).ConfigureAwait(false);
        await SaveFanArtImagesAsync(images.Id, images.Banners, language, mediaItemId, name, FanArtTypes.Banner).ConfigureAwait(false);
        await SaveFanArtImagesAsync(images.Id, images.Covers, language, mediaItemId, name, FanArtTypes.Cover).ConfigureAwait(false);
        if (includeThumbnails)
          await SaveFanArtImagesAsync(images.Id, images.Thumbnails, language, mediaItemId, name, FanArtTypes.Thumbnail).ConfigureAwait(false);
        if (!OnlyBasicFanArt)
        {
          await SaveFanArtImagesAsync(images.Id, images.ClearArt, language, mediaItemId, name, FanArtTypes.ClearArt).ConfigureAwait(false);
          await SaveFanArtImagesAsync(images.Id, images.DiscArt, language, mediaItemId, name, FanArtTypes.DiscArt).ConfigureAwait(false);
          await SaveFanArtImagesAsync(images.Id, images.Logos, language, mediaItemId, name, FanArtTypes.Logo).ConfigureAwait(false);
        }
        Logger.Debug(_id + " Download: Finished saving images for {0} [{1}]", info, mediaItemId);
        return true;
      }
      catch (WebException)
      {
        //Remote server probably returned an error/not found, just log at debug level
        Logger.Debug(_id + " Download: WebException when downloading images for {0} [{1}]", info, mediaItemId);
      }
      catch (Exception ex)
      {
        Logger.Warn(_id + " Download: Failed downloading images for {0} [{1}]", ex, info, mediaItemId);
      }
      return false;
    }

    protected virtual bool VerifyFanArtImage(TImg image, TLang language, string fanArtType)
    {
      return image != null;
    }

    protected virtual async Task<int> SaveFanArtImagesAsync(string id, IEnumerable<TImg> images, TLang language, Guid mediaItemId, string name, string fanArtType)
    {
      if (images == null || !images.Any())
        return 0;

      var validImages = images.Where(i => VerifyFanArtImage(i, language, fanArtType)).ToList();      
      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      int count = await fanArtCache.TrySaveFanArt(mediaItemId, name, fanArtType, validImages, (p, i) => SaveFanArtImageAsync(id, i, p, mediaItemId, name)).ConfigureAwait(false);

      Logger.Debug(_id + @" Download: Saved {0} for media item {1} ({2}) of type {3}", count, mediaItemId, name, fanArtType);
      return count;
    }

    private async Task<bool> SaveFanArtImageAsync(string id, TImg image, string path, Guid mediaItemId, string name)
    {
      try
      {
        await _wrapper.DownloadFanArtAsync(id, image, path).ConfigureAwait(false);
        return true;
      }
      catch (Exception ex)
      {
        Logger.Debug(_id + " Download: Exception downloading images for ID {0} [{1} ({2})]", ex, id, mediaItemId, name);
        return false;
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
