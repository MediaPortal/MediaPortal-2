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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.NfoReaders;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors
{
  public class AudioFanArtHandler : BaseFanArtHandler
  {
    #region Constants

    private static readonly Guid[] FANART_ASPECTS = { AudioAspect.ASPECT_ID };
    private static readonly NfoAudioExtractor AUDIO_EXTRACTOR = new NfoAudioExtractor();

    /// <summary>
    /// GUID string for the nfo audio FanArt handler.
    /// </summary>
    public const string FANARTHANDLER_ID_STR = "D8A8E144-EC1B-42B3-AFFE-18864DAE38DA";

    /// <summary>
    /// Nfo audio FanArt handler GUID.
    /// </summary>
    public static Guid FANARTHANDLER_ID = new Guid(FANARTHANDLER_ID_STR);

    #endregion

    public AudioFanArtHandler()
      : base(new FanArtHandlerMetadata(FANARTHANDLER_ID, "NFO Audio FanArt handler"), FANART_ASPECTS)
    {
    }

    public override async Task CollectFanArtAsync(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IResourceLocator mediaItemLocator = null;
      if (!BaseInfo.IsVirtualResource(aspects))
        mediaItemLocator = GetResourceLocator(aspects);

      if (!aspects.ContainsKey(AudioAspect.ASPECT_ID) || mediaItemLocator == null)
        return;

      IFanArtCache fanArtCache = ServiceRegistration.Get<IFanArtCache>();
      using (IResourceAccessor mediaItemAccessor = mediaItemLocator.CreateAccessor())
      {
        //Album fanart
        if (RelationshipExtractorUtils.TryGetLinkedId(AudioAlbumAspect.ROLE_ALBUM, aspects, out Guid albumMediaItemId) && AddToCache(albumMediaItemId))
        {
          var existingCovers = fanArtCache.GetFanArtFiles(albumMediaItemId, FanArtTypes.Cover);
          if (!existingCovers.Any()) //Only get album cover if needed for better performance
          {
            NfoAlbumReader albumNfoReader = await AUDIO_EXTRACTOR.TryGetNfoAlbumReaderAsync(mediaItemAccessor, true).ConfigureAwait(false);
            if (albumNfoReader != null)
            {
              var stubs = albumNfoReader.GetAlbumStubs();
              var mainStub = stubs?.FirstOrDefault();
              if (mainStub?.Thumb != null)
              {
                await fanArtCache.TrySaveFanArt(albumMediaItemId, mainStub.Title, FanArtTypes.Cover, p => TrySaveFileImage(mainStub.Thumb, p, "Thumb", "Nfo.")).ConfigureAwait(false);
              }
            }
          }
        }

        //Artist fanart
        IList<Tuple<Guid, string>> artists = GetArtists(aspects);
        if (artists?.Count > 0)
        {
          foreach (var artist in artists)
          {
            var existingThumbs = fanArtCache.GetFanArtFiles(artist.Item1, FanArtTypes.Thumbnail);
            if (!existingThumbs.Any() && AddToCache(artist.Item1)) //Only get artist thumbnail if needed for better performance
            {
              NfoArtistReader artistReader = await AUDIO_EXTRACTOR.TryGetNfoArtistReaderAsync(mediaItemAccessor, artist.Item2, true).ConfigureAwait(false);
              if (artistReader != null)
              {
                var stubs = artistReader.GetArtistStubs();
                var mainStub = stubs?.FirstOrDefault();
                if (string.Equals(mainStub?.Name, artist.Item2, StringComparison.InvariantCultureIgnoreCase))
                {
                  if (mainStub?.Thumb != null)
                  {
                    await fanArtCache.TrySaveFanArt(artist.Item1, artist.Item2, FanArtTypes.Thumbnail, p => TrySaveFileImage(mainStub.Thumb, p, "Thumb", "Nfo.")).ConfigureAwait(false);
                  }
                }
              }
            }
          }
        }
      }
    }

    #region Protected methods

    protected IList<Tuple<Guid, string>> GetArtists(IDictionary<Guid, IList<MediaItemAspect>> aspects)
    {
      IList<Tuple<Guid, string>> artists = null;
      if (MediaItemAspect.TryGetAspect(aspects, AudioAspect.Metadata, out SingleMediaItemAspect audioAspect))
      {
        var artistNames = audioAspect.GetCollectionAttribute<string>(AudioAspect.ATTR_ALBUMARTISTS);
        if (artistNames != null)
          RelationshipExtractorUtils.TryGetMappedLinkedIds(PersonAspect.ROLE_ALBUMARTIST, aspects, artistNames.ToList(), out artists);
      }
      return artists;
    }

    #endregion
  }

  public class NfoAudioExtractor : NfoAudioExtractorBase
  {
    public new Task<NfoAlbumReader> TryGetNfoAlbumReaderAsync(IResourceAccessor mediaItemAccessor, bool includeFanart)
    {
      return base.TryGetNfoAlbumReaderAsync(mediaItemAccessor, includeFanart);
    }

    public new Task<NfoArtistReader> TryGetNfoArtistReaderAsync(IResourceAccessor mediaItemAccessor, string artistName, bool includeFanart)
    {
      return base.TryGetNfoArtistReaderAsync(mediaItemAccessor, artistName, includeFanart);
    }
  }
}
