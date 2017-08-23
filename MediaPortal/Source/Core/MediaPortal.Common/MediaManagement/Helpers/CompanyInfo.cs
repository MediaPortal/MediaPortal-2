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

using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="CompanyInfo"/> contains metadata information about a company item.
  /// </summary>
  public class CompanyInfo : BaseInfo, IComparable<CompanyInfo>
  {
    /// <summary>
    /// Contains the ids of the minimum aspects that need to be present in order to test the equality of instances of this item.
    /// </summary>
    public static Guid[] EQUALITY_ASPECTS = new[] { CompanyAspect.ASPECT_ID, ExternalIdentifierAspect.ASPECT_ID, MediaAspect.ASPECT_ID };
    /// <summary>
    /// Gets or sets the company IMDB id.
    /// </summary>
    public string ImdbId = null;
    /// <summary>
    /// Gets or sets the company TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    public int MovieDbId = 0;
    public int TvMazeId = 0;
    public string MusicBrainzId = null;
    public long AudioDbId = 0;
    public string NameId = null;

    /// <summary>
    /// Gets or sets the company name.
    /// </summary>
    public string Name = null;
    public SimpleTitle Description = null;
    public string Type = null;
    public int? Order = null;

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (string.IsNullOrEmpty(Name))
          return false;
        if (string.IsNullOrEmpty(Type))
          return false;

        return true;
      }
    }

    public override bool HasExternalId
    {
      get
      {
        if (TvdbId > 0)
          return true;
        if (MovieDbId > 0)
          return true;
        if (TvMazeId > 0)
          return true;
        if (AudioDbId > 0)
          return true;
        if (!string.IsNullOrEmpty(MusicBrainzId))
          return true;

        return false;
      }
    }

    public override void AssignNameId()
    {
      if (!string.IsNullOrEmpty(Name))
      {
        NameId = GetNameId(Name);
      }
    }

    public CompanyInfo Clone()
    {
      return CloneProperties(this);
    }

    #region Members

    /// <summary>
    /// Copies the contained company information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Name)) return false;
      if (string.IsNullOrEmpty(Type)) return false;

      SetMetadataChanged(aspectData);

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_SORT_TITLE, GetSortTitle(Name));
      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_ISVIRTUAL, false); //Always based on physical media
      MediaItemAspect.SetAttribute(aspectData, CompanyAspect.ATTR_COMPANY_NAME, Name);
      if (!Description.IsEmpty) MediaItemAspect.SetAttribute(aspectData, CompanyAspect.ATTR_DESCRIPTION, CleanString(Description.Text));
      MediaItemAspect.SetAttribute(aspectData, CompanyAspect.ATTR_COMPANY_TYPE, Type);

      if (Type == CompanyAspect.COMPANY_TV_NETWORK)
      {
        if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_NETWORK, ImdbId);
        if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_NETWORK, TvdbId.ToString());
        if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_NETWORK, MovieDbId.ToString());
        if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_NETWORK, MusicBrainzId);
        if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_NETWORK, AudioDbId.ToString());
        if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_NETWORK, TvMazeId.ToString());
        if (!string.IsNullOrEmpty(NameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_NETWORK, NameId);
      }
      else
      {
        if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_COMPANY, ImdbId);
        if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_COMPANY, TvdbId.ToString());
        if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COMPANY, MovieDbId.ToString());
        if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_COMPANY, MusicBrainzId);
        if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_COMPANY, AudioDbId.ToString());
        if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_COMPANY, TvMazeId.ToString());
        if (!string.IsNullOrEmpty(NameId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_COMPANY, NameId);
      }

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public override bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(CompanyAspect.ASPECT_ID))
        return false;

      GetMetadataChanged(aspectData);

      MediaItemAspect.TryGetAttribute(aspectData, CompanyAspect.ATTR_COMPANY_NAME, out Name);
      MediaItemAspect.TryGetAttribute(aspectData, CompanyAspect.ATTR_COMPANY_TYPE, out Type);

      string tempString;
      MediaItemAspect.TryGetAttribute(aspectData, CompanyAspect.ATTR_DESCRIPTION, out tempString);
      Description = new SimpleTitle(tempString, false);

      if (Type == CompanyAspect.COMPANY_TV_NETWORK)
      {
        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_NETWORK, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_NETWORK, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_NETWORK, out id))
          AudioDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_NETWORK, out id))
          TvMazeId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_NETWORK, out ImdbId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_NETWORK, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_NETWORK, out NameId);
      }
      else
      {
        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_COMPANY, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COMPANY, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_COMPANY, out id))
          AudioDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_COMPANY, out id))
          TvMazeId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_COMPANY, out ImdbId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_COMPANY, out MusicBrainzId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_NAME, ExternalIdentifierAspect.TYPE_COMPANY, out NameId);
      }

      byte[] data;
      if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
        HasThumbnail = true;

      return true;
    }

    public override bool FromString(string name)
    {
      Name = name;
      return true;
    }

    public override bool CopyIdsFrom<T>(T otherInstance)
    {
      if (otherInstance == null)
        return false;

      if (otherInstance is CompanyInfo)
      {
        CompanyInfo otherCompany = otherInstance as CompanyInfo;

        AudioDbId = otherCompany.AudioDbId;
        ImdbId = otherCompany.ImdbId;
        MovieDbId = otherCompany.MovieDbId;
        MusicBrainzId = otherCompany.MusicBrainzId;
        TvdbId = otherCompany.TvdbId;
        TvMazeId = otherCompany.TvMazeId;
        NameId = otherCompany.NameId;
        return true;
      }
      return false;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return string.IsNullOrEmpty(Name) ? "[Unnamed Company]" : Name;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      if (string.IsNullOrEmpty(NameId))
        AssignNameId();
      return string.IsNullOrEmpty(NameId) ? "[Unnamed Company]".GetHashCode() : NameId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      CompanyInfo other = obj as CompanyInfo;
      if (other == null) return false;

      if (TvdbId > 0 && other.TvdbId > 0 && Type == other.Type)
        return TvdbId == other.TvdbId;
      if (MovieDbId > 0 && other.MovieDbId > 0 && Type == other.Type)
        return MovieDbId == other.MovieDbId;
      if (AudioDbId > 0 && other.AudioDbId > 0 && Type == other.Type)
        return AudioDbId == other.AudioDbId;
      if (!string.IsNullOrEmpty(MusicBrainzId) && !string.IsNullOrEmpty(other.MusicBrainzId) && Type == other.Type)
        return string.Equals(MusicBrainzId, other.MusicBrainzId, StringComparison.InvariantCultureIgnoreCase);

      //Name id is generated from name and can be unreliable so should only be used if matches
      if (!string.IsNullOrEmpty(NameId) && !string.IsNullOrEmpty(other.NameId) &&
        string.Equals(NameId, other.NameId, StringComparison.InvariantCultureIgnoreCase))
        return true;

      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && MatchNames(Name, other.Name) && Type == other.Type)
        return true;

      return false;
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(CompanyInfo))
      {
        CompanyInfo info = new CompanyInfo();
        info.CopyIdsFrom(this);
        info.Name = Name;
        info.Type = Type;
        return (T)(object)info;
      }
      return default(T);
    }

    public int CompareTo(CompanyInfo other)
    {
      if (Order != other.Order)
      {
        if (!Order.HasValue) return 1;
        if (!other.Order.HasValue) return -1;
        return Order.Value.CompareTo(other.Order.Value);
      }

      return Name.CompareTo(other.Name);
    }

    #endregion
  }
}
