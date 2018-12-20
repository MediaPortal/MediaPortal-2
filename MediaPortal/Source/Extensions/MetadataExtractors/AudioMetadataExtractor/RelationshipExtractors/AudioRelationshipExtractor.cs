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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.MediaManagement.TransientAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.OnlineLibraries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  public class AudioRelationshipExtractor : IRelationshipExtractor
  {
    #region Constants

    /// <summary>
    /// GUID string for the audio relationship metadata extractor.
    /// </summary>
    public const string METADATAEXTRACTOR_ID_STR = "C50A6923-55B7-4596-B097-3885CFE7C7EC";

    /// <summary>
    /// Audio relationship metadata extractor GUID.
    /// </summary>
    public static Guid METADATAEXTRACTOR_ID = new Guid(METADATAEXTRACTOR_ID_STR);

    #endregion

    protected RelationshipExtractorMetadata _metadata;
    private IList<IRelationshipRoleExtractor> _extractors;

    public AudioRelationshipExtractor()
    {
      _metadata = new RelationshipExtractorMetadata(METADATAEXTRACTOR_ID, "Audio relationship extractor", MetadataExtractorPriority.Core);
      RegisterRelationships();
      InitExtractors();
    }

    protected void InitExtractors()
    {
      _extractors = new List<IRelationshipRoleExtractor>();

      _extractors.Add(new TrackAlbumArtistRelationshipExtractor());
      _extractors.Add(new TrackArtistRelationshipExtractor());
      _extractors.Add(new TrackComposerRelationshipExtractor());
      _extractors.Add(new TrackConductorRelationshipExtractor());
      _extractors.Add(new TrackAlbumRelationshipExtractor());
      _extractors.Add(new AlbumArtistRelationshipExtractor());
      _extractors.Add(new AlbumLabelRelationshipExtractor());
    }

    /// <summary>
    /// Registers all relationships that are extracted by this relationship extractor.
    /// </summary>
    protected void RegisterRelationships()
    {
      IRelationshipTypeRegistration relationshipRegistration = ServiceRegistration.Get<IRelationshipTypeRegistration>();

      //Relationships must be registered in order from tracks up to all parent relationships

      //Hierarchical relationships
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Album", true,
        AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, AudioAspect.ASPECT_ID, AudioAlbumAspect.ASPECT_ID,
        AudioAspect.ATTR_TRACK, AudioAlbumAspect.ATTR_AVAILABLE_TRACKS, true), true);

      //Simple (non hierarchical) relationships
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Artist", AudioAspect.ROLE_TRACK, PersonAspect.ROLE_ARTIST), true);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Album Artist", AudioAspect.ROLE_TRACK, PersonAspect.ROLE_ALBUMARTIST), true);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Composer", AudioAspect.ROLE_TRACK, PersonAspect.ROLE_COMPOSER), true);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Track->Conductor", AudioAspect.ROLE_TRACK, PersonAspect.ROLE_CONDUCTOR), true);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Album->Artist", AudioAlbumAspect.ROLE_ALBUM, PersonAspect.ROLE_ALBUMARTIST), false);
      relationshipRegistration.RegisterLocallyKnownRelationshipType(new RelationshipType("Album->Label", AudioAlbumAspect.ROLE_ALBUM, CompanyAspect.ROLE_MUSIC_LABEL), false);
    }

    public IDictionary<IFilter, uint> GetLastChangedItemsFilters()
    {
      Dictionary<IFilter, uint> filters = new Dictionary<IFilter, uint>();

      //Add filters for audio albums
      //We need to find audio tracks because importer only works with files
      //The relationship extractor for albums should then do the update
      List<AlbumInfo> changedAlbums = OnlineMatcherService.Instance.GetLastChangedAudioAlbums();
      foreach (AlbumInfo album in changedAlbums)
      {
        Dictionary<string, string> ids = new Dictionary<string, string>();
        if (album.AudioDbId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_AUDIODB, album.AudioDbId.ToString());
        if (!string.IsNullOrEmpty(album.AmazonId))
          ids.Add(ExternalIdentifierAspect.SOURCE_AMAZON, album.AmazonId);
        if (!string.IsNullOrEmpty(album.CdDdId))
          ids.Add(ExternalIdentifierAspect.SOURCE_CDDB, album.CdDdId);
        if (!string.IsNullOrEmpty(album.ItunesId))
          ids.Add(ExternalIdentifierAspect.SOURCE_ITUNES, album.ItunesId);
        if (!string.IsNullOrEmpty(album.MusicBrainzGroupId))
          ids.Add(ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, album.MusicBrainzGroupId);
        if (!string.IsNullOrEmpty(album.MusicBrainzId))
          ids.Add(ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, album.MusicBrainzId);
        if (!string.IsNullOrEmpty(album.UpcEanId))
          ids.Add(ExternalIdentifierAspect.SOURCE_UPCEAN, album.UpcEanId);

        IFilter albumChangedFilter = null;
        foreach (var id in ids)
        {
          if (albumChangedFilter == null)
          {
            albumChangedFilter = new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_ALBUM),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
              });
          }
          else
          {
            albumChangedFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, albumChangedFilter,
            new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_ALBUM),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
            }));
          }
        }

        if (albumChangedFilter != null)
          filters.Add(new FilteredRelationshipFilter(AudioAspect.ROLE_TRACK, AudioAlbumAspect.ROLE_ALBUM, albumChangedFilter), 1);
      }

      //Add filters for changed audio tracks
      List<TrackInfo> changedTracks = OnlineMatcherService.Instance.GetLastChangedAudio();
      foreach (TrackInfo track in changedTracks)
      {
        Dictionary<string, string> ids = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(track.IsrcId))
          ids.Add(ExternalIdentifierAspect.SOURCE_ISRC, track.IsrcId);
        if (track.AudioDbId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_AUDIODB, track.AudioDbId.ToString());
        if (track.LyricId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_LYRIC, track.LyricId.ToString());
        if (!string.IsNullOrEmpty(track.MusicBrainzId))
          ids.Add(ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, track.MusicBrainzId);
        if (!string.IsNullOrEmpty(track.MusicIpId))
          ids.Add(ExternalIdentifierAspect.SOURCE_MUSIC_IP, track.MusicIpId);
        if (track.MvDbId > 0)
          ids.Add(ExternalIdentifierAspect.SOURCE_MVDB, track.MvDbId.ToString());

        IFilter trackChangedFilter = null;
        foreach (var id in ids)
        {
          if (trackChangedFilter == null)
          {
            trackChangedFilter = new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_TRACK),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
              });
          }
          else
          {
            trackChangedFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, trackChangedFilter,
            new BooleanCombinationFilter(BooleanOperator.And, new[]
            {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, id.Key),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, ExternalIdentifierAspect.TYPE_TRACK),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id.Value),
            }));
          }
        }

        if (trackChangedFilter != null)
          filters.Add(trackChangedFilter, 1);
      }

      return filters;
    }

    public void ResetLastChangedItems()
    {
      OnlineMatcherService.Instance.ResetLastChangedAudioAlbums();
      OnlineMatcherService.Instance.ResetLastChangedAudio();
    }

    public IDictionary<Guid, IList<MediaItemAspect>> GetBaseChildAspectsFromExistingAspects(IDictionary<Guid, IList<MediaItemAspect>> existingChildAspects, IDictionary<Guid, IList<MediaItemAspect>> existingParentAspects)
    {
      if (existingParentAspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
      {
        AlbumInfo album = new AlbumInfo();
        album.FromMetadata(existingParentAspects);

        if (existingChildAspects.ContainsKey(AudioAspect.ASPECT_ID))
        {
          TrackInfo track = new TrackInfo();
          track.FromMetadata(existingChildAspects);

          TrackInfo basicTrack = album.CloneBasicInstance<TrackInfo>();
          basicTrack.TrackNum = track.TrackNum;
          IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
          basicTrack.SetMetadata(aspects, true);
          return aspects;
        }
      }
      return null;
    }

    public RelationshipExtractorMetadata Metadata
    {
      get { return _metadata; }
    }

    public IList<IRelationshipRoleExtractor> RoleExtractors
    {
      get { return _extractors; }
    }
  }
}
