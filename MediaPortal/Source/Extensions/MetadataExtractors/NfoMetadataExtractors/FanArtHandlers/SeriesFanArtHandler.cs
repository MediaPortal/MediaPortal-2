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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Stubs;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  public class SeriesFanArtHandler : BaseFanArtHandler
  {
    #region Constants

    private static readonly Guid[] FANART_ASPECTS = { EpisodeAspect.ASPECT_ID };
    private static readonly NfoSeriesExtractor SERIES_EXTRACTOR = new NfoSeriesExtractor();

    /// <summary>
    /// GUID string for the nfo series FanArt handler.
    /// </summary>
    public const string FANARTHANDLER_ID_STR = "F3D7E786-2BBE-4234-9E92-1D9DA306924C";

    /// <summary>
    /// Nfo Episode FanArt handler GUID.
    /// </summary>
    public static Guid FANARTHANDLER_ID = new Guid(FANARTHANDLER_ID_STR);

    #endregion

    #region Constructor

    public SeriesFanArtHandler()
      : base(new FanArtHandlerMetadata(FANARTHANDLER_ID, "NFO Series FanArt handler"), FANART_ASPECTS)
    {
    }

    #endregion

    #region Base overrides

    public override async Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IResourceLocator mediaItemLocator = null;
      if (!BaseInfo.IsVirtualResource(aspects))
        mediaItemLocator = GetResourceLocator(aspects);

      if (!aspects.ContainsKey(EpisodeAspect.ASPECT_ID) || mediaItemLocator == null)
        return;

      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      using (IResourceAccessor mediaItemAccessor = mediaItemLocator.CreateAccessor())
      {
        EpisodeInfo episodeInfo = new EpisodeInfo();
        if (!episodeInfo.FromMetadata(aspects))
          return;

        //Episode fanart
        if (AddToCache(mediaItemId))
        {
          var existingThumbs = fanArtCache.GetFanArtFiles(mediaItemId, FanArtTypes.Thumbnail);
          int? season = episodeInfo.SeasonNumber;
          int? episode = episodeInfo.EpisodeNumbers != null && episodeInfo.EpisodeNumbers.Any() ? episodeInfo.EpisodeNumbers.First() : (int?)null;
          if (!existingThumbs.Any()) //Only get thumb if needed for better performance
          {
            NfoSeriesEpisodeReader episodeReader = await SERIES_EXTRACTOR.TryGetNfoSeriesEpisodeReaderAsync(mediaItemAccessor, season, episode, true).ConfigureAwait(false);
            if (episodeReader != null)
            {
              var stubs = episodeReader.GetEpisodeStubs();
              var mainStub = stubs?.FirstOrDefault();
              if (mainStub?.Thumb != null)
              {
                await fanArtCache.TrySaveFanArt(mediaItemId, episodeInfo.ToString(), FanArtTypes.Thumbnail, p => TrySaveFileImage(mainStub.Thumb, p, "Thumb", "Nfo.")).ConfigureAwait(false);
              }
            }
          }
        }

        //Series fanart
        if (RelationshipExtractorUtils.TryGetLinkedId(SeriesAspect.ROLE_SERIES, aspects, out Guid seriesMediaItemId))
        {
          IList<Tuple<Guid, string>> actors = GetActors(aspects);
          RelationshipExtractorUtils.TryGetLinkedId(SeasonAspect.ROLE_SEASON, aspects, out Guid seasonMediaItemId);

          //Check if loading nfo is needed
          if ((actors?.All(a => IsInCache(a.Item1)) ?? true) && IsInCache(seriesMediaItemId) && (seasonMediaItemId == Guid.Empty || IsInCache(seasonMediaItemId)))
            return; //Everything was already saved

          NfoSeriesReader seriesNfoReader = await SERIES_EXTRACTOR.TryGetNfoSeriesReaderAsync(mediaItemAccessor, true).ConfigureAwait(false);
          if (seriesNfoReader != null)
          {
            var stubs = seriesNfoReader.GetSeriesStubs();
            var mainStub = stubs?.FirstOrDefault();
            if (AddToCache(seriesMediaItemId))
            {
              var series = episodeInfo.CloneBasicInstance<SeriesInfo>();
              if (mainStub?.Thumbs?.Count > 0)
                await TrySaveThumbStubs(fanArtCache, mainStub.Thumbs, null, seriesMediaItemId, series.ToString());
            }

            if (seasonMediaItemId != Guid.Empty && episodeInfo.SeasonNumber.HasValue && AddToCache(seasonMediaItemId))
            {
              var season = episodeInfo.CloneBasicInstance<SeasonInfo>();
              if (mainStub?.Thumbs?.Count > 0)
                await TrySaveThumbStubs(fanArtCache, mainStub.Thumbs, episodeInfo.SeasonNumber, seasonMediaItemId, season.ToString());
            }


            //Actor fanart
            //We only want the series actors because thumb loading is disabled on episode actors for performance reasons, so we might need to
            //load the series nfo multiple time before we have all actors depending on what actors are in the episode
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

    protected async Task<bool> TrySaveThumbStubs(IFanArtCache fanArtCache, HashSet<SeriesThumbStub> thumbs, int? season, Guid mediaItemId, string mediaItemName)
    {
      if (thumbs == null || thumbs.Count == 0)
        return false;

      HashSet<byte[]> posters = new HashSet<byte[]>();
      HashSet<byte[]> banners = new HashSet<byte[]>();
      HashSet<byte[]> fanart = new HashSet<byte[]>();
      foreach (var thumbStub in thumbs)
      {
        if ((!season.HasValue && thumbStub.Season == null) || (season.HasValue && thumbStub.Season == season))
        {
          if (thumbStub.Aspect == SeriesThumbStub.ThumbAspect.Poster || !thumbStub.Aspect.HasValue)
            posters.Add(thumbStub.Thumb);
          else if (thumbStub.Aspect == SeriesThumbStub.ThumbAspect.Banner)
            banners.Add(thumbStub.Thumb);
          else if (thumbStub.Aspect == SeriesThumbStub.ThumbAspect.Fanart)
            fanart.Add(thumbStub.Thumb);
        }
      }

      await TrySaveFanArt(fanArtCache, FanArtTypes.Poster, "Poster", posters, mediaItemId, mediaItemName);
      await TrySaveFanArt(fanArtCache, FanArtTypes.Banner, "Banner", banners, mediaItemId, mediaItemName);
      await TrySaveFanArt(fanArtCache, FanArtTypes.FanArt, "FanArt", fanart, mediaItemId, mediaItemName);

      return true;
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

  public class NfoSeriesExtractor : NfoSeriesExtractorBase
  {
    public new Task<NfoSeriesEpisodeReader> TryGetNfoSeriesEpisodeReaderAsync(IResourceAccessor mediaItemAccessor, int? season, int? episode, bool includeFanart)
    {
      return base.TryGetNfoSeriesEpisodeReaderAsync(mediaItemAccessor, season, episode, includeFanart);
    }

    public new Task<NfoSeriesReader> TryGetNfoSeriesReaderAsync(IResourceAccessor mediaItemAccessor, bool includeFanart)
    {
      return base.TryGetNfoSeriesReaderAsync(mediaItemAccessor, includeFanart);
    }
  }
}
