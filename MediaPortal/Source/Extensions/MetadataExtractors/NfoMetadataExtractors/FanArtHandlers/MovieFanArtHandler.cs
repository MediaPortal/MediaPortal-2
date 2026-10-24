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

using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Common;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  public class MovieFanArtHandler : BaseFanArtHandler
  {
    #region Constants

    private static readonly Guid[] FANART_ASPECTS = { MovieAspect.ASPECT_ID };
    private static readonly NfoMovieExtractor MOVIE_EXTRACTOR = new NfoMovieExtractor();

    /// <summary>
    /// Folder within a film's folder that holds further backdrops, as the media managers name it.
    /// </summary>
    private const string EXTRA_FANART_FOLDER = "ExtraFanArt/";

    /// <summary>
    /// GUID string for the nfo movie FanArt handler.
    /// </summary>
    public const string FANARTHANDLER_ID_STR = "183DD0B4-C742-46E4-A771-E606165A1CC8";

    /// <summary>
    /// Movie FanArt handler GUID.
    /// </summary>
    public static Guid FANARTHANDLER_ID = new Guid(FANARTHANDLER_ID_STR);

    #endregion

    #region Members

    /// <summary>
    /// The film reader's settings, read once and kept up to date while the server runs.
    /// </summary>
    private SettingsChangeWatcher<NfoMovieMetadataExtractorSettings> _settingsWatcher;

    #endregion

    #region Constructor

    public MovieFanArtHandler()
      : base(new FanArtHandlerMetadata(FANARTHANDLER_ID, "NFO Movie FanArt handler"), FANART_ASPECTS)
    {
    }

    #endregion

    #region Base overrides

    public override async Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IResourceLocator mediaItemLocator = null;
      if (!BaseInfo.IsVirtualResource(aspects))
        mediaItemLocator = GetResourceLocator(aspects);

      if (!aspects.ContainsKey(MovieAspect.ASPECT_ID) || mediaItemLocator == null)
        return;

      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      using (IResourceAccessor mediaItemAccessor = mediaItemLocator.CreateAccessor())
      {
        IList<Tuple<Guid, string>> actors = GetActors(aspects);

        //Check if loading nfo is needed
        if ((actors?.All(a => IsInCache(a.Item1)) ?? true) && IsInCache(mediaItemId))
          return; //Everything was already saved

        // The pictures a media manager left next to the film come first, and whether they are copied at all is
        // the two questions the reference asks of every extractor: is the film on a local disk, and did the
        // household say it wants those copied. A shelf of films without a single .nfo file is common enough
        // that the walk must not hang off the file being there.
        if (CachesFolderPictures(mediaItemLocator))
        {
          ResourcePath filmDirectory = ResourcePathHelper.Combine(mediaItemLocator.NativeResourcePath, "../");
          await SaveFolderPicturesAsync(mediaItemLocator.NativeSystemId, filmDirectory, mediaItemId,
              GetTitle(aspects, mediaItemLocator),
              ResourcePathHelper.GetFileNameWithoutExtension(mediaItemLocator.NativeResourcePath.ToString())
                  .ToLowerInvariant(), EXTRA_FANART_FOLDER).ConfigureAwait(false);
        }

        NfoMovieReader movieNfoReader = await MOVIE_EXTRACTOR.TryGetNfoMovieReaderAsync(mediaItemAccessor, true).ConfigureAwait(false);
        if (movieNfoReader != null)
        {
          //Movie fanart
          var stubs = movieNfoReader.GetMovieStubs();
          var mainStub = stubs?.FirstOrDefault();
          if (AddToCache(mediaItemId))
          {
            if (mainStub?.Thumb != null)
            {
              await fanArtCache.TrySaveFanArt(mediaItemId, mainStub.Title, FanArtTypes.Poster, p => TrySaveFileImage(mainStub.Thumb, p, "Thumb", "Nfo.")).ConfigureAwait(false);
            }

            await TrySaveFanArt(fanArtCache, FanArtTypes.FanArt, "FanArt", mainStub?.FanArt, mediaItemId, mainStub?.Title).ConfigureAwait(false);
            await TrySaveFanArt(fanArtCache, FanArtTypes.DiscArt, "DiscArt", mainStub?.DiscArt, mediaItemId, mainStub?.Title).ConfigureAwait(false);
            await TrySaveFanArt(fanArtCache, FanArtTypes.Logo, "Logo", mainStub?.Logos, mediaItemId, mainStub?.Title).ConfigureAwait(false);
            await TrySaveFanArt(fanArtCache, FanArtTypes.ClearArt, "ClearArt", mainStub?.ClearArt, mediaItemId, mainStub?.Title).ConfigureAwait(false);
            await TrySaveFanArt(fanArtCache, FanArtTypes.Banner, "Banner", mainStub?.Banners, mediaItemId, mainStub?.Title).ConfigureAwait(false);
            await TrySaveFanArt(fanArtCache, FanArtTypes.Thumbnail, "Landscape", mainStub?.Landscape, mediaItemId, mainStub?.Title).ConfigureAwait(false);
          }

          //Actor fanart
          if (actors != null)
            foreach (var actor in actors)
            {
              if (!IsInCache(actor.Item1))
              {
                var existingThumbs = fanArtCache.GetFanArtFiles(actor.Item1, FanArtTypes.Thumbnail);
                var actorStub = mainStub?.Actors?.FirstOrDefault(a => string.Equals(a?.Name, actor.Item2, StringComparison.InvariantCultureIgnoreCase));
                if (actorStub != null || existingThumbs.Any()) //We have a thumb already or no thumb is available, so no need to check again
                  AddToCache(actor.Item1);

                if (actorStub?.Thumb != null)
                {
                  await fanArtCache.TrySaveFanArt(actor.Item1, actor.Item2, FanArtTypes.Thumbnail, p => TrySaveFileImage(actorStub.Thumb, p, "Thumb", "Nfo.")).ConfigureAwait(false);
                }
              }
            }
        }
      }
    }

    #endregion

    #region Protected methods

    protected IList<Tuple<Guid, string>> GetActors(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IList<Tuple<Guid, string>> actors = null;
      if (MediaItemAspect.TryGetAspect(aspects, VideoAspect.Metadata, out SingleMediaItemAspect videoAspect))
      {
        var actorNames = videoAspect.GetCollectionAttribute<string>(VideoAspect.ATTR_ACTORS);
        if (actorNames != null)
          RelationshipExtractorUtils.TryGetMappedLinkedIds(PersonAspect.ROLE_ACTOR, aspects, actorNames.ToList(), out actors);
      }
      return actors;
    }

    /// <summary>
    /// Whether the pictures lying next to a film are to be copied into the cache.
    /// </summary>
    /// <param name="mediaItemLocator">Where the film stands.</param>
    /// <returns><c>true</c> when the share the film stands on is one the household has asked to be copied.</returns>
    /// <remarks>
    /// This is the question the reference asks before it walks a folder, answered by the two switches the film
    /// reader carries: one for a disk plugged into this machine, one for a share that might be asleep later.
    /// </remarks>
    protected bool CachesFolderPictures(IResourceLocator mediaItemLocator)
    {
      NfoMovieMetadataExtractorSettings settings = GetSettings();
      return ShouldCacheLocalFanArt(mediaItemLocator.NativeResourcePath, settings.CacheLocalFanArt,
          settings.CacheOfflineFanArt);
    }

    /// <summary>
    /// The film reader's settings, watched, so that a switch thrown on the settings screen is seen by the next
    /// title the importer reaches rather than waiting for the server to come back.
    /// </summary>
    protected NfoMovieMetadataExtractorSettings GetSettings()
    {
      SettingsChangeWatcher<NfoMovieMetadataExtractorSettings> watcher = _settingsWatcher;
      if (watcher == null)
      {
        watcher = new SettingsChangeWatcher<NfoMovieMetadataExtractorSettings>();
        SettingsChangeWatcher<NfoMovieMetadataExtractorSettings> existing =
            Interlocked.CompareExchange(ref _settingsWatcher, watcher, null);
        if (existing != null)
        {
          watcher.Dispose();
          watcher = existing;
        }
      }
      return watcher.Settings;
    }

    /// <summary>
    /// The name written beside the pictures, which is what the film is called rather than what its file is
    /// called. The cache puts this into the folder's index and the screens show it back.
    /// </summary>
    protected static string GetTitle(IDictionary<Guid, IList<MediaItemAspect>> aspects, IResourceLocator locator)
    {
      if (MediaItemAspect.TryGetAspect(aspects, MediaAspect.Metadata, out SingleMediaItemAspect media))
      {
        string title = media.GetAttributeValue<string>(MediaAspect.ATTR_TITLE);
        if (!string.IsNullOrEmpty(title))
          return title;
      }
      return ResourcePathHelper.GetFileNameWithoutExtension(locator.NativeResourcePath.ToString());
    }

    protected async Task<bool> TrySaveFanArt(IFanArtCache fanArtCache, string fanArtType, string fanArtName, HashSet<byte[]> fanArtData, Guid mediaItemId, string mediaName)
    {
      if (fanArtData == null || fanArtData.Count == 0)
        return false;

      bool addCount = fanArtData.Count > 1;
      int count = 0;
      foreach (var data in fanArtData)
        await fanArtCache.TrySaveFanArt(mediaItemId, mediaName, fanArtType, p => TrySaveFileImage(data, p, $"{fanArtName}{(addCount ? (count++).ToString() : "")}", "Nfo."));

      return true;
    }

    #endregion
  }

  public class NfoMovieExtractor : NfoMovieExtractorBase
  {
    public new Task<NfoMovieReader> TryGetNfoMovieReaderAsync(IResourceAccessor mediaItemAccessor, bool includeFanart)
    {
      return base.TryGetNfoMovieReaderAsync(mediaItemAccessor, includeFanart);
    }
  }
}
