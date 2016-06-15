#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

    /// <summary>
    /// Gets or sets the company name.
    /// </summary>
    public string Name = null;
    public LanguageText Description = null;
    public string Type = null;
    public int? Order = null;

    #region Members

    /// <summary>
    /// Copies the contained company information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (string.IsNullOrEmpty(Name)) return false;
      if (string.IsNullOrEmpty(Type)) return false;

      MediaItemAspect.SetAttribute(aspectData, MediaAspect.ATTR_TITLE, ToString());
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
      }
      else
      {
        if (!string.IsNullOrEmpty(ImdbId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_COMAPANY, ImdbId);
        if (TvdbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_COMAPANY, TvdbId.ToString());
        if (MovieDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COMAPANY, MovieDbId.ToString());
        if (!string.IsNullOrEmpty(MusicBrainzId)) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_COMAPANY, MusicBrainzId);
        if (AudioDbId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_COMAPANY, AudioDbId.ToString());
        if (TvMazeId > 0) MediaItemAspect.AddOrUpdateExternalIdentifier(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_COMAPANY, TvMazeId.ToString());
      }

      SetThumbnailMetadata(aspectData);

      return true;
    }

    public bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(CompanyAspect.ASPECT_ID))
        return false;

      MediaItemAspect.TryGetAttribute(aspectData, CompanyAspect.ATTR_COMPANY_NAME, out Name);
      MediaItemAspect.TryGetAttribute(aspectData, CompanyAspect.ATTR_COMPANY_TYPE, out Type);

      string tempString;
      MediaItemAspect.TryGetAttribute(aspectData, CompanyAspect.ATTR_DESCRIPTION, out tempString);
      Description = new LanguageText(tempString, false);

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
      }
      else
      {
        string id;
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVDB, ExternalIdentifierAspect.TYPE_COMAPANY, out id))
          TvdbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_COMAPANY, out id))
          MovieDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_AUDIODB, ExternalIdentifierAspect.TYPE_COMAPANY, out id))
          AudioDbId = Convert.ToInt32(id);
        if (MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_TVMAZE, ExternalIdentifierAspect.TYPE_COMAPANY, out id))
          TvMazeId = Convert.ToInt32(id);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_COMAPANY, out ImdbId);
        MediaItemAspect.TryGetExternalAttribute(aspectData, ExternalIdentifierAspect.SOURCE_MUSICBRAINZ, ExternalIdentifierAspect.TYPE_COMAPANY, out MusicBrainzId);
      }

      byte[] data;
      if (MediaItemAspect.TryGetAttribute(aspectData, ThumbnailLargeAspect.ATTR_THUMBNAIL, out data))
        Thumbnail = data;

      return true;
    }

    public bool FromString(string name)
    {
      Name = name;
      return true;
    }

    public bool CopyIdsFrom(CompanyInfo otherCompany)
    {
      AudioDbId = otherCompany.AudioDbId;
      ImdbId = otherCompany.ImdbId;
      MovieDbId = otherCompany.MovieDbId;
      MusicBrainzId = otherCompany.MusicBrainzId;
      TvdbId = otherCompany.TvdbId;
      TvMazeId = otherCompany.TvMazeId;
      return true;
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return Name;
    }

    public override bool Equals(object obj)
    {
      CompanyInfo other = obj as CompanyInfo;
      if (obj == null) return false;
      if (TvdbId > 0 && TvdbId == other.TvdbId && Type == other.Type) return true;
      if (MovieDbId > 0 && MovieDbId == other.MovieDbId && Type == other.Type) return true;
      if (AudioDbId > 0 && AudioDbId == other.AudioDbId && Type == other.Type) return true;
      if (!string.IsNullOrEmpty(MusicBrainzId) && !string.IsNullOrEmpty(other.MusicBrainzId) && 
        string.Equals(MusicBrainzId, other.MusicBrainzId, StringComparison.InvariantCultureIgnoreCase) && 
        Type == other.Type)
        return true;
      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(other.Name) && 
        MatchNames(Name, other.Name) && Type == other.Type)
        return true;

      return false;
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
