#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// GUID string for the nfo movie FanArt handler.
    /// </summary>
    public const string FANARTHANDLER_ID_STR = "183DD0B4-C742-46E4-A771-E606165A1CC8";

    /// <summary>
    /// Movie FanArt handler GUID.
    /// </summary>
    public static Guid FANARTHANDLER_ID = new Guid(FANARTHANDLER_ID_STR);

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
              await fanArtCache.TrySaveFanArt(mediaItemId, mainStub.Title, FanArtTypes.Poster, p => TrySaveFileImage(stubs.First().Thumb, p, "Thumb", "Nfo.")).ConfigureAwait(false);
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
                var actorStub = mainStub?.Actors?.FirstOrDefault(a => string.Equals(a.Name, actor.Item2, StringComparison.InvariantCultureIgnoreCase));
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
