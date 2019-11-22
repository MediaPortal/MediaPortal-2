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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.TransientAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Utilities;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// <see cref="SubtitleInfo"/> contains metadata information about a subtitle item.
  /// </summary>
  public class SubtitleInfo : BaseInfo, IComparable<SubtitleInfo>
  {
    /// <summary>
    /// Contains the ids of the minimum aspects that need to be present in order to test the equality of instances of this item.
    /// </summary>
    public static Guid[] EQUALITY_ASPECTS = new[] { SubtitleAspect.ASPECT_ID };
    /// <summary>
    /// Gets or sets the subtitles media IMDB id.
    /// </summary>
    public string ImdbId = null;
    /// <summary>
    /// Gets or sets the subtitles media TheTvDB id.
    /// </summary>
    public int TvdbId = 0;
    /// <summary>
    /// Gets or sets the subtitles media MovieDB id.
    /// </summary>
    public int MovieDbId = 0;
    public string SubtitleId = null;
    public string NameId = null;
    public Dictionary<string, string> CustomIds = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets the subtitle file name.
    /// </summary>
    public string Name = null;
    public string DisplayName = null;
    public List<IResourceLocator> MediaFiles = new List<IResourceLocator>();
    public List<string> Categories = new List<string>();

    /// <summary>
    /// Search result
    /// </summary>
    public int? MatchPercentage = null;
    public int? LanguageMatchRank = null;

    //Subtitle info
    public string MediaTitle = null;
    public int? Year = null;
    public int? Season = null;
    public int? Episode = null;
    public string Language = null;

    public override bool IsBaseInfoPresent
    {
      get
      {
        if (!string.IsNullOrEmpty(Name))
          return true;

        return false;
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
        if (!string.IsNullOrEmpty(ImdbId))
          return true;
        if (CustomIds.Any())
          return true;

        return false;
      }
    }

    public override void AssignNameId()
    {
      if (!string.IsNullOrEmpty(Name))
      {
        //Give the subtitle a fallback Id so it will always be created
        NameId = Name;
      }
    }

    public SubtitleInfo Clone()
    {
      SubtitleInfo clone = (SubtitleInfo)this.MemberwiseClone();
      return clone;
    }

    public override bool MergeWith(object other, bool overwriteShorterStrings = true, bool updatePrimaryChildList = false)
    {
      if (other is SubtitleInfo sub)
      {
        SubtitleId = $"{SubtitleId};{sub.SubtitleId}";
        Name = $"{Name};{sub.Name}";
        if (MatchPercentage.HasValue && sub.MatchPercentage.HasValue)
          MatchPercentage = Math.Max(MatchPercentage.Value, sub.MatchPercentage.Value);
        else if (sub.MatchPercentage.HasValue)
          MatchPercentage = sub.MatchPercentage;

        HasChanged |= MetadataUpdater.SetOrUpdateId(ref TvdbId, sub.TvdbId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref ImdbId, sub.ImdbId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref MovieDbId, sub.MovieDbId);
        HasChanged |= MetadataUpdater.SetOrUpdateId(ref CustomIds, sub.CustomIds);

        Categories = Categories.Except(sub.Categories).Concat(sub.Categories).ToList();

        MergeDataProviders(sub);
        return true;
      }
      return false;
    }

    #region Members

    /// <summary>
    /// Copies the contained person information into MediaItemAspect.
    /// </summary>
    /// <param name="aspectData">Dictionary with extracted aspects.</param>
    public override bool SetMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData, bool force = false)
    {
      if (!force && !IsBaseInfoPresent)
        return false;

      SingleMediaItemAspect subtitleAspect = MediaItemAspect.GetOrCreateAspect(aspectData, TempSubtitleAspect.Metadata);
      subtitleAspect.SetAttribute(TempSubtitleAspect.ATTR_PROVIDER, string.Join(";", DataProviders));
      subtitleAspect.SetAttribute(TempSubtitleAspect.ATTR_CATEGORY, string.Join(";", Categories));
      subtitleAspect.SetAttribute(TempSubtitleAspect.ATTR_NAME, Name);
      subtitleAspect.SetAttribute(TempSubtitleAspect.ATTR_DISPLAY_NAME, DisplayName);
      subtitleAspect.SetAttribute(TempSubtitleAspect.ATTR_SUBTITLEID, SubtitleId);
      subtitleAspect.SetAttribute(TempSubtitleAspect.ATTR_LANGUAGE, Language);

      int idx = 0;
      foreach(var mediaFile in MediaFiles)
      {
        MultipleMediaItemAspect resourceAspect = MediaItemAspect.CreateAspect(aspectData, ProviderResourceAspect.Metadata);
        resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, idx++);
        resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
        resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "subtitle/unknown");
        resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, mediaFile.NativeSystemId);
        resourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, mediaFile.NativeResourcePath.Serialize());
      }

      return true;
    }

    public override bool FromMetadata(IDictionary<Guid, IList<MediaItemAspect>> aspectData)
    {
      if (!aspectData.ContainsKey(TempSubtitleAspect.ASPECT_ID) && !aspectData.ContainsKey(EpisodeAspect.ASPECT_ID) && !aspectData.ContainsKey(MovieAspect.ASPECT_ID))
        return false;

      if (MediaItemAspect.TryGetAspect(aspectData, EpisodeAspect.Metadata, out var episodeAspect))
      {
        IEnumerable collection;
        if (MediaItemAspect.TryGetAttribute(aspectData, EpisodeAspect.ATTR_EPISODE, out collection))
          Episode = collection.Cast<int>().Distinct().First();

        Season = episodeAspect.GetAttributeValue<int>(EpisodeAspect.ATTR_SEASON);
        MediaTitle = episodeAspect.GetAttributeValue<string>(EpisodeAspect.ATTR_SERIES_NAME);
      }
      else if (MediaItemAspect.TryGetAspect(aspectData, MovieAspect.Metadata, out var movieAspect))
      {
        MediaTitle = movieAspect.GetAttributeValue<string>(MovieAspect.ATTR_MOVIE_NAME);
        if (MediaItemAspect.TryGetAttribute(aspectData, MediaAspect.ATTR_RECORDINGTIME, out DateTime release))
          Year = release.Year;
      }
      else if (MediaItemAspect.TryGetAspect(aspectData, MediaAspect.Metadata, out var mediaAspect))
      {
        MediaTitle = movieAspect.GetAttributeValue<string>(MediaAspect.ATTR_TITLE);
      }

      if (MediaItemAspect.TryGetAspect(aspectData, TempSubtitleAspect.Metadata, out var subAspect))
      {
        Name = subAspect.GetAttributeValue<string>(TempSubtitleAspect.ATTR_NAME);
        DisplayName = subAspect.GetAttributeValue<string>(TempSubtitleAspect.ATTR_DISPLAY_NAME);
        SubtitleId = subAspect.GetAttributeValue<string>(TempSubtitleAspect.ATTR_SUBTITLEID);
        Language = subAspect.GetAttributeValue<string>(TempSubtitleAspect.ATTR_LANGUAGE);

        DataProviders.Clear();
        var dataProviders = subAspect.GetAttributeValue<string>(TempSubtitleAspect.ATTR_PROVIDER);
        if (dataProviders?.Count() > 0)
          DataProviders = new List<string>(dataProviders.Split(';'));

        Categories.Clear();
        var categories = subAspect.GetAttributeValue<string>(TempSubtitleAspect.ATTR_CATEGORY);
        if (categories?.Count() > 0)
          Categories = new List<string>(categories.Split(';'));
      }

      MediaFiles.Clear();
      if (aspectData.ContainsKey(ProviderResourceAspect.ASPECT_ID))
      {
        IList<MultipleMediaItemAspect> resourceAspects;
        if (MediaItemAspect.TryGetAspects(aspectData, ProviderResourceAspect.Metadata, out resourceAspects))
        {
          foreach (MultipleMediaItemAspect resourceAspect in resourceAspects)
          {
            string systemId = resourceAspect.GetAttributeValue<string>(ProviderResourceAspect.ATTR_SYSTEM_ID);
            string path = resourceAspect.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
            int type = resourceAspect.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE);
            if (type == ProviderResourceAspect.TYPE_PRIMARY)
              MediaFiles.Add(new ResourceLocator(systemId, ResourcePath.Deserialize(path)));
          }
        }
      }

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

      if (otherInstance is SubtitleInfo)
      {
        SubtitleInfo other = otherInstance as SubtitleInfo;
        MovieDbId = other.MovieDbId;
        ImdbId = other.ImdbId;
        TvdbId = other.TvdbId;
        SubtitleId = other.SubtitleId;
        foreach (var keyVal in other.CustomIds)
          CustomIds[keyVal.Key] = keyVal.Value;
        return true;
      }
      else if (otherInstance is MovieInfo)
      {
        MovieInfo other = otherInstance as MovieInfo;
        MovieDbId = other.MovieDbId;
        ImdbId = other.ImdbId;
        foreach (var keyVal in other.CustomIds)
          CustomIds[keyVal.Key] = keyVal.Value;
        return true;
      }
      else if (otherInstance is EpisodeInfo)
      {
        EpisodeInfo other = otherInstance as EpisodeInfo;
        MovieDbId = other.MovieDbId;
        ImdbId = other.ImdbId;
        TvdbId = other.TvdbId;
        foreach (var keyVal in other.CustomIds)
          CustomIds[keyVal.Key] = keyVal.Value;
        return true;
      }
      return false;
    }

    public override T CloneBasicInstance<T>()
    {
      if (typeof(T) == typeof(SubtitleInfo))
      {
        SubtitleInfo info = new SubtitleInfo();
        info.CopyIdsFrom(this);
        info.Name = Name;
        info.NameId = NameId;
        return (T)(object)info;
      }
      return default(T);
    }

    #endregion

    #region Overrides

    public override string ToString()
    {
      return string.IsNullOrEmpty(Name) ? "[Unnamed Subtitle]" : Name;
    }

    public override int GetHashCode()
    {
      //TODO: Check if this is functional
      if (string.IsNullOrEmpty(NameId))
        AssignNameId();
      return string.IsNullOrEmpty(NameId) ? "[Unnamed Subtitle]".GetHashCode() : NameId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      SubtitleInfo other = obj as SubtitleInfo;
      if (other == null) return false;

      if (!string.IsNullOrEmpty(SubtitleId) && !string.IsNullOrEmpty(other.SubtitleId))
        return string.Equals(SubtitleId, other.SubtitleId, StringComparison.InvariantCultureIgnoreCase);
      if (TvdbId > 0 && other.TvdbId > 0)
        return TvdbId == other.TvdbId;
      if (MovieDbId > 0 && other.MovieDbId > 0)
        return MovieDbId == other.MovieDbId;
      if (!string.IsNullOrEmpty(ImdbId) && !string.IsNullOrEmpty(other.ImdbId))
        return string.Equals(ImdbId, other.ImdbId, StringComparison.InvariantCultureIgnoreCase);
      foreach (var key in CustomIds.Keys)
      {
        if (other.CustomIds.ContainsKey(key))
          return string.Equals(CustomIds[key], other.CustomIds[key], StringComparison.InvariantCultureIgnoreCase);
      }

      //Name id is generated from name and can be unreliable so should only be used if matches
      if (!string.IsNullOrEmpty(NameId) && !string.IsNullOrEmpty(other.NameId) &&
        string.Equals(NameId, other.NameId, StringComparison.InvariantCultureIgnoreCase))
        return true;

      return false;
    }

    public bool StrictMatchNames(string name1, string name2)
    {
      return CompareNames(name1, name2);
    }

    public int CompareTo(SubtitleInfo other)
    {
      return Name.CompareTo(other.Name);
    }

    #endregion
  }
}
