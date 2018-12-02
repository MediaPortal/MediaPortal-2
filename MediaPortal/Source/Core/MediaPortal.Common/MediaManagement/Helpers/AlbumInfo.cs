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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System.Text.RegularExpressions;
using System.Collections;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="AlbumInfo"/> contains information about an album. It's used as an interface structure for external 
  /// online data scrapers to fill in metadata.
  /// </summary>
  public class AlbumInfo : BaseInfo
  {
    /// <summary>
    /// Contains the ids of the minimum aspects that need to be present in order to accurately test the equality of instances of this item.
    /// </summary>
    public static Guid[] EQUALITY_ASPECTS = new[] { AudioAlbumAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
    /// <summary>
    /// Returns the index for "Album" used in <see cref="FormatString"/>.
    /// </summary>
    public static int ALBUM_INDEX = 0;
    /// <summary>
    /// Returns the index for "Year" used in <see cref="FormatString"/>.
    /// </summary>
    public static int ALBUM_YEAR_INDEX = 1;
    /// <summary>
    /// Format string that holds album name including release year.
    /// </summary>
    public static string ALBUM_FORMAT_STR = "{0} ({1})";
    /// <summary>
    /// Short format string that holds album name.
    /// </summary>
    public static string SHORT_FORMAT_STR = "{0}";

    protected static Regex _fromName = new Regex(@"(?<album>.*) \((?<year>\d+)\)", RegexOptions.IgnoreCase);
    protected static Regex _albumNumber = new Regex(@"(?<number>\d+)$", RegexOptions.IgnoreCase);

    public string MusicBrainzId = null;
    public string MusicBrainzGroupId = null;
    public string MusicBrainzDiscId = null;
    public long AudioDbId = 0;
    public string CdDdId = null;
    public string UpcEanId = null;
    public string AmazonId = null;
    public string ItunesId = null;
    public string NameId = null;

    public string Album = null;
    public string AlbumSort = null;
    public SimpleTitle Description = null;
    public DateTime? ReleaseDate = null;
    public int TotalTracks = 0;
    public int DiscNum = 0;
    public int TotalDiscs = 0;
    public SimpleRating Rating = new SimpleRating();
    public long Sales = 0;
    public bool Compilation = false;
    public bool HasOnlineCover = false;
    public bool HasBarcode = false;

    public List<PersonInfo> Artists = new List<PersonInfo>();
    public List<CompanyInfo> MusicLabels = new List<CompanyInfo>();
    public List<GenreInfo> Genres = new List<GenreInfo>();
    public List<string> Awards = new List<string>();
    public List<string> Languages = new List<string>();
    public List<TrackInfo> Tracks = new List<TrackInfo>();

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (string.IsNullOrEmpty(Album))
          return false;

        return true;
      }
    }

    public override bool HasExternalId
    {
      get
      {
        if (AudioDbId > 0)
          return true;
        if (!string.IsNullOrEmpty(MusicBrainzId))
          return true;
        if (!string.IsNullOrEmpty(MusicBrainzGroupId))
          return true;
        if (!string.IsNullOrEmpty(CdDdId))
          return true;
        if (!string.IsNullOrEmpty(UpcEanId))
          return true;
        if (!string.IsNullOrEmpty(AmazonId))
          return true;
        if (!string.IsNullOrEmpty(ItunesId))
          return true;

        return false;
      }
    }

    public override void AssignNameId()
    {
      if (!string.IsNullOrEmpty(Album))
      {
        //Give the album a fallback Id so it will always be created
        if (Artists.Count > 0)
          NameId = Artists[0].Name + ":" + Album;
        else
          NameId = Album;
        NameId = GetNameId(NameId);
      }
    }

    public AlbumInfo Clone()
    {
      AlbumInfo clone = (AlbumInfo)this.MemberwiseClone();
      clone.Description = new SimpleTitle(Description.Text, Description.DefaultLanguage);
      clone.Rating = new SimpleRating(Rating.RatingValue, Rating.VoteCount);
      clone.Languages = new List<string>();
      foreach (var l in Languages)
        clone.Languages.Add(l);
      clone.Awards = new List<string>();
      foreach (var a in Awards)
        clone.Awards.Add(a);
      clone.Artists = new List<PersonInfo>();
      foreach (var a in Artists)
        clone.Artists.Add(a.Clone());
      clone.Tracks = new List<TrackInfo>();
      foreach (var d in Tracks)
        clone.Tracks.Add(d.Clone());
      clone.MusicLabels = new List<CompanyInfo>();
      foreach (var l in MusicLabels)
        clone.MusicLabels.Add(l.Clone());
      clone.Genres = new List<GenreInfo>();
      foreach (var g in Genres)
        clone.Genres.Add(new GenreInfo() { Id = g.Id, Name = g.Name });

      return clone;
    }

    public override bool MergeWith(object other, bool overwriteShorterStrings = false, bool updateTrackList = false)
    {
      if (other is AlbumInfo album)
      {
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref AudioDbId, album.AudioDbId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref CdDdId, album.CdDdId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref MusicBrainzDiscId, album.MusicBrainzDiscId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref MusicBrainzGroupId, album.MusicBrainzGroupId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref MusicBrainzId, album.MusicBrainzId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref AmazonId, album.AmazonId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref ItunesId, album.ItunesId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref UpcEanId, album.UpcEanId);

        HasChanged |= MetadataUpdater.SetOrUpdateString(ref Album, album.Album, overwriteShorterStrings);
        HasChanged |= MetadataUpdater.SetOrUpdateString(ref Description, album.Description, overwriteShorterStrings);

        if (TotalTracks < album.TotalTracks)
        {
          HasChanged = true;
          TotalTracks = album.TotalTracks;
        }

        HasChanged |= MetadataUpdater.SetOrUpdateValue(ref Compilation, album.Compilation);
        HasChanged |= MetadataUpdater.SetOrUpdateValue(ref DiscNum, album.DiscNum);
        HasChanged |= MetadataUpdater.SetOrUpdateValue(ref ReleaseDate, album.ReleaseDate);
        HasChanged |= MetadataUpdater.SetOrUpdateValue(ref Sales, album.Sales);
        HasChanged |= MetadataUpdater.SetOrUpdateValue(ref TotalDiscs, album.TotalDiscs);

        HasChanged |= MetadataUpdater.SetOrUpdateRatings(ref Rating, album.Rating);

        if (Genres.Count == 0)
        {
          HasChanged |= MetadataUpdater.SetOrUpdateList(Genres, album.Genres.Distinct().ToList(), true);
        }
        HasChanged |= MetadataUpdater.SetOrUpdateList(Awards, album.Awards.Distinct().ToList(), true);

        //These lists contain Ids and other properties that are not persisted, so they will always appear changed.
        //So changes to these lists will only be stored if something else has changed.
        MetadataUpdater.SetOrUpdateList(Artists, album.Artists.Where(p => !string.IsNullOrEmpty(p.Name)).Distinct().ToList(), Artists.Count == 0, overwriteShorterStrings);
        MetadataUpdater.SetOrUpdateList(MusicLabels, album.MusicLabels.Where(c => !string.IsNullOrEmpty(c.Name)).Distinct().ToList(), MusicLabels.Count == 0, overwriteShorterStrings);

        if (updateTrackList) //Comparing all tracks can be quite time consuming
        {
          MetadataUpdater.SetOrUpdateList(Tracks, album.Tracks.Distinct().ToList(), true, overwriteShorterStrings);
          List<string> artists = new List<string>();
          foreach (TrackInfo track in album.Tracks)
          {
            if (track.Artists.Count > 0)
              if (!artists.Contains(track.Artists[0].Name))
                artists.Add(track.Artists[0].Name);
          }
          if (album.Tracks.Count > 5 && (float)artists.Count > (float)album.Tracks.Count * 0.6 && !Compilation)
          {
            Compilation = true;
            HasChanged = true;
          }
        }

        if (Artists.Count > 0 && !Compilation &&
          (Artists[0].Name.IndexOf("Various", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
          Artists[0].Name.Equals("VA", StringComparison.InvariantCultureIgnoreCase)))
        {
          Compilation = true;
          HasChanged = true;
        }
        return true;
      }
      return false;
    }

    #region Members

    /// <summary>
    /// Copies the contained track information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData, bool force = false)
    {
      if (!force && !IsBaseInfoPresent)
        return false;

      AssignNameId();
      SetMetadataChanged(aspectData);

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      if (!string.IsNullOrEmpty(AlbumSort)) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, AlbumSort);
      //MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, true); //Is maintained by medialibrary and metadataextractors
      MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_ALBUM, Album);
      MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_COMPILATION, Compilation);
      if (!Description.IsEmpty) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_DESCRIPTION, Description.Text);
      if (DiscNum > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_DISCID, DiscNum);
      if (TotalDiscs > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMDISCS, TotalDiscs);
      if (ReleaseDate.HasValue) MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, ReleaseDate.Value);
      if (TotalTracks > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMTRACKS, TotalTracks);
      if (Sales > 0) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_SALES, Sales);

      if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, MusicBrainzId);
      if (!string.IsNullOrEmpty(MusicBrainzGroupId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, MusicBrainzGroupId);
      if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, AudioDbId.ToString());
      if (!string.IsNullOrEmpty(CdDdId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, CdDdId);
      if (!string.IsNullOrEmpty(UpcEanId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, UpcEanId);
      if (!string.IsNullOrEmpty(AmazonId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AMAZON, ExternalIdentifierAspect.TYPE_ALBUM, AmazonId);
      if (!string.IsNullOrEmpty(ItunesId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_ITUNES, ExternalIdentifierAspect.TYPE_ALBUM, ItunesId);
      if (!string.IsNullOrEmpty(NameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_ALBUM, NameId);

      if (!Rating.IsEmpty)
      {
        MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_TOTAL_RATING, Rating.RatingValue.Value);
        if (Rating.VoteCount.HasValue) MediaItemAspect.SetAttribute(aspectData, AudioAlbumAspect.ATTR_RATING_COUNT, Rating.VoteCount.Value);
      }

      if (Artists.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_ARTISTS, Artists.Where(p => !string.IsNullOrEmpty(p.Name)).Select(p => p.Name).Distinct().ToList());

      if (Awards.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_AWARDS, Awards.Where(a => !string.IsNullOrEmpty(a)).Distinct().ToList());

      if (MusicLabels.Count > 0) MediaItemAspect.SetCollectionAttribute(aspectData, AudioAlbumAspect.ATTR_LABELS, MusicLabels.Where(l => !string.IsNullOrEmpty(l.Name)).Select(l => l.Name).Distinct().ToList());

      aspectData.Remove(GenreAspect.ASPECT_ID);
      foreach (GenreInfo genre in Genres.Distinct())
      {
        MultipleMediaItemAspect genreAspect = MediaItemAspect.CreateAspect(aspectData, GenreAspect.Metadata);
        genreAspect.SetAttribute(GenreAspect.ATTR_ID, genre.Id);
        genreAspect.SetAttribute(GenreAspect.ATTR_GENRE, genre.Name);
      }

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public override bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      GetMetadataChanged(aspectData);

      if (aspectData.ContainsKey(AudioAlbumAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_ALBUM, out Album);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, out AlbumSort);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_DISCID, out DiscNum);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMDISCS, out TotalDiscs);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_NUMTRACKS, out TotalTracks);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_COMPILATION, out Compilation);
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out ReleaseDate);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_SALES, out Sales);

        string tempString;
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_DESCRIPTION, out tempString);
        Description = new SimpleTitle(tempString, string.IsNullOrWhiteSpace(tempString));

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
          AudioDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzGroupId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out CdDdId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, out UpcEanId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AMAZON, ExternalIdentifierAspect.TYPE_ALBUM, out AmazonId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_ITUNES, ExternalIdentifierAspect.TYPE_ALBUM, out ItunesId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_ALBUM, out NameId);

        double? rating;
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_TOTAL_RATING, out rating);
        int? voteCount;
        MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_RATING_COUNT, out voteCount);
        Rating = new SimpleRating(rating, voteCount);

        //Brownard 17.06.2016
        //The returned type of the collection differs on the server and client.
        //On the server it's an object collection but on the client it's a string collection due to [de]serialization.
        //Use the non generic Ienumerable to allow for both types.
        IEnumerable collection;
        Artists.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_ARTISTS, out collection))
          Artists.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_ARTIST, ParentMediaName = Album }));
        foreach (PersonInfo artist in Artists)
          artist.AssignNameId();

        Awards.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_AWARDS, out collection))
          Awards.AddRange(collection.Cast<string>());

        MusicLabels.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAlbumAspect.ATTR_LABELS, out collection))
          MusicLabels.AddRange(collection.Cast<string>().Select(s => new CompanyInfo { Name = s, Type = CompanyAspect.COMPANY_MUSIC_LABEL }));
        foreach (CompanyInfo company in MusicLabels)
          company.AssignNameId();

        Genres.Clear();
        IList<MultipleMediaItemAspect> genreAspects;
        if(MediaItemAspect.TryGetAspects(aspectData, GenreAspect.Metadata, out genreAspects))
        {
          foreach (MultipleMediaItemAspect genre in genreAspects)
          {
            Genres.Add(new GenreInfo
            {
              Id = genre.GetAttributeValue<int?>(GenreAspect.ATTR_ID),
              Name = genre.GetAttributeValue<string>(GenreAspect.ATTR_GENRE)
            });
          }
        }

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          HasThumbnail = true;

        return true;
      }
      else if (aspectData.ContainsKey(AudioAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ALBUM, out Album);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_DISCID, out DiscNum);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_NUMDISCS, out TotalDiscs);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_NUMTRACKS, out TotalTracks);
        MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_COMPILATION, out Compilation);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
          AudioDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzGroupId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out CdDdId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, out UpcEanId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AMAZON, ExternalIdentifierAspect.TYPE_ALBUM, out AmazonId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_ITUNES, ExternalIdentifierAspect.TYPE_ALBUM, out ItunesId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_ALBUM, out NameId);

        IEnumerable collection;
        Artists.Clear();
        if (MediaItemAspect.TryGetAttribute(aspectData, AudioAspect.ATTR_ALBUMARTISTS, out collection))
          Artists.AddRange(collection.Cast<string>().Select(s => new PersonInfo { Name = s, Occupation = PersonAspect.OCCUPATION_ARTIST, ParentMediaName = Album }));
        foreach (PersonInfo artist in Artists)
          artist.AssignNameId();

        return true;
      }
      else if (aspectData.ContainsKey(MediaAspect.ASPECT_ID))
      {
        MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_TITLE, out Album);

        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_ALBUM, out id))
          AudioDbId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ_GROUP, ExternalIdentifierAspect.TYPE_ALBUM, out MusicBrainzGroupId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_CDDB, ExternalIdentifierAspect.TYPE_ALBUM, out CdDdId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_UPCEAN, ExternalIdentifierAspect.TYPE_ALBUM, out UpcEanId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AMAZON, ExternalIdentifierAspect.TYPE_ALBUM, out AmazonId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_ITUNES, ExternalIdentifierAspect.TYPE_ALBUM, out ItunesId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_ALBUM, out NameId);

        byte[] data;
        if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
          HasThumbnail = true;
      }
      return false;
    }

    public string ToShortString()
    {
      return string.Format(SHORT_FORMAT_STR, Album);
    }

    public override bool FromString(string name)
    {
      if (name.Contains("("))
      {
        Match match = _fromName.Match(name);
        if (match.Success)
        {
          Album = match.Groups["album"].Value;
          int year = Convert.ToInt32(match.Groups["year"].Value);
          if (year > 0)
            ReleaseDate = new DateTime(year, 1, 1);
          return true;
        }
        return false;
      }
      Album = name;
      return true;
    }

    public override bool CopyIdsFrom<T>(T otherInstance)
    {
      if (otherInstance == null)
        return false;

      if (otherInstance is AlbumInfo)
      {
        AlbumInfo otherAlbum = otherInstance as AlbumInfo;
        AudioDbId = otherAlbum.AudioDbId;
        CdDdId = otherAlbum.CdDdId;
        MusicBrainzDiscId = otherAlbum.MusicBrainzDiscId;
        MusicBrainzGroupId = otherAlbum.MusicBrainzGroupId;
        MusicBrainzId = otherAlbum.MusicBrainzId;
        AmazonId = otherAlbum.AmazonId;
        ItunesId = otherAlbum.ItunesId;
        NameId = otherAlbum.NameId;
        return true;
      }
      else if (otherInstance is TrackInfo)
      {
        TrackInfo albumTrack = otherInstance as TrackInfo;
        AudioDbId = albumTrack.AlbumAudioDbId;
        CdDdId = albumTrack.AlbumCdDdId;
        MusicBrainzDiscId = albumTrack.AlbumMusicBrainzDiscId;
        MusicBrainzGroupId = albumTrack.AlbumMusicBrainzGroupId;
        MusicBrainzId = albumTrack.AlbumMusicBrainzId;
        AmazonId = albumTrack.AlbumAmazonId;
        ItunesId = albumTrack.AlbumItunesId;
        NameId = albumTrack.AlbumNameId;
        return true;
      }
      return false;
    }

    /// <summary>
    /// If album volume info is present, determines whether both albums represent the same volume.
    /// </summary>
    /// <param name="other">The album to match against.</param>
    /// <returns><c>true</c> if the volumes match or no volume info is present.</returns>
    public bool AlbumVolumesAreEqual(AlbumInfo other)
    {
      if (!string.IsNullOrEmpty(Album) && !string.IsNullOrEmpty(other.Album))
      {
        Match match = _albumNumber.Match(Album);
        if (match.Success)
        {
          string searchNumber = match.Groups["number"].Value;
          match = _albumNumber.Match(other.Album);
          if (match.Success && searchNumber != match.Groups["number"].Value)
            return false;
        }
      }
      return true;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      if (ReleaseDate.HasValue)
        return string.Format(ALBUM_FORMAT_STR, string.IsNullOrEmpty(Album) ? "Unnamed Album" : Album, ReleaseDate.Value.Year);
      return string.IsNullOrEmpty(Album) ? "[Unnamed Album]" : Album;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      if (string.IsNullOrEmpty(NameId))
        AssignNameId();
      return string.IsNullOrEmpty(NameId) ? "[Unnamed Album]".GetHashCode() : NameId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      AlbumInfo other = obj as AlbumInfo;
      if (other == null) return false;

      //For albums, the name is likely to have come from a tag so ensure that names are similar in addition
      //to the checks below, so that if a user has 2 albums in different qualities, deliberately tagged differently
      //they don't get merged into the same album.
      if (!string.IsNullOrEmpty(Album) && !string.IsNullOrEmpty(other.Album) && !MatchNames(Album, other.Album))
        return false;

      if (AudioDbId > 0 && other.AudioDbId > 0)
        return AudioDbId == other.AudioDbId;
      
      if (!string.IsNullOrEmpty(CdDdId) && !string.IsNullOrEmpty(other.CdDdId))
        return string.Equals(CdDdId, other.CdDdId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(UpcEanId) && !string.IsNullOrEmpty(other.UpcEanId))
        return string.Equals(UpcEanId, other.UpcEanId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(AmazonId) && !string.IsNullOrEmpty(other.AmazonId))
        return string.Equals(AmazonId, other.AmazonId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(ItunesId) && !string.IsNullOrEmpty(other.ItunesId))
        return string.Equals(ItunesId, other.ItunesId, StringComparison.InvariantCultureIgnoreCase);

      //Name id is generated from name and can be unreliable so should only be used if matches
      if (!string.IsNullOrEmpty(NameId) && !string.IsNullOrEmpty(other.NameId) &&
        string.Equals(NameId, other.NameId, StringComparison.InvariantCultureIgnoreCase))
        return true;

      if (!string.IsNullOrEmpty(MusicBrainzId) && !string.IsNullOrEmpty(other.MusicBrainzId))
        return string.Equals(MusicBrainzId, other.MusicBrainzId, StringComparison.InvariantCultureIgnoreCase);
      if (!string.IsNullOrEmpty(MusicBrainzGroupId) && !string.IsNullOrEmpty(other.MusicBrainzGroupId))
        return string.Equals(MusicBrainzGroupId, other.MusicBrainzGroupId, StringComparison.InvariantCultureIgnoreCase);

      if (ReleaseDate.HasValue && other.ReleaseDate.HasValue &&
        (Artists == null || Artists.Count == 0 || other.Artists == null || other.Artists.Count == 0 || Artists[0].Equals(other.Artists[0])))
        return ReleaseDate.Value.Year == other.ReleaseDate.Value.Year;

      return AlbumVolumesAreEqual(other);
    }

    public override bool MatchNames(string name1, string name2)
    {
      return CompareNames(name1, name2, 0.8, 3);
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(AlbumInfo))
      {
        AlbumInfo info = new AlbumInfo();
        info.CopyIdsFrom(this);
        info.Album = Album;
        info.AlbumSort = AlbumSort;
        info.ReleaseDate = ReleaseDate;
        foreach(var artist in Artists)
          info.Artists.Add(artist.Clone());
        return (T)(object)info;
      }
      else if (typeof(T) == typeof(TrackInfo))
      {
        TrackInfo info = new TrackInfo();
        info.CopyIdsFrom(this);
        info.Album = Album;
        foreach (var artist in Artists)
          info.AlbumArtists.Add(artist.Clone());
        return (T)(object)info;
      }
      return default(T);
    }

    #endregion
  }
}
