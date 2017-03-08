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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  public abstract class IAudioRelationshipExtractor
  {
    protected static RelationshipCache _relationshipCache = null;
    protected HashSet<BaseInfo> _checkCache = new HashSet<BaseInfo>();

    static IAudioRelationshipExtractor()
    {
      _relationshipCache = new RelationshipCache("AudioRelationshipCache", TimeSpan.FromHours(AudioMetadataExtractor.MINIMUM_HOUR_AGE_BEFORE_UPDATE));
    }

    public virtual void ClearCache()
    {
      _relationshipCache.Clear();
      _checkCache.Clear();
    }

    protected virtual bool AddToCache<T>(Guid mediaItemId, T mediaItemInfo, bool createCopy = true) where T : BaseInfo
    {
      return _relationshipCache.AddToCache(mediaItemId, mediaItemInfo, createCopy);
    }

    protected virtual bool TryGetIdFromCache(BaseInfo mediaItemInfo, out Guid mediaItemId)
    {
      return _relationshipCache.TryGetIdFromCache(mediaItemInfo, out mediaItemId);
    }

    protected virtual bool TryGetInfoFromCache<T>(T mediaItemInfo, out T cachedInfo, out Guid mediaItemId) where T : BaseInfo
    {
      return _relationshipCache.TryGetInfoFromCache(mediaItemInfo, out cachedInfo, out mediaItemId);
    }

    protected virtual bool AddToCheckCache<T>(T mediaItemInfo) where T : BaseInfo
    {
      T cacheItem = mediaItemInfo.CloneBasicInstance<T>();
      return cacheItem != null && _checkCache.Add(cacheItem);
    }

    protected virtual bool CheckCacheContains(BaseInfo mediaItemInfo)
    {
      return _checkCache.Contains(mediaItemInfo);
    }

    public static IFilter GetTrackSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> trackFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(AudioAspect.ASPECT_ID))
        return null;

      int trackFilter = -1;
      int albumFilter = -1;
      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        //Track filter
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_TRACK)
          {
            if (trackFilter < 0)
            {
              trackFilter = trackFilters.Count;
              trackFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              trackFilters[trackFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, trackFilters[trackFilter],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }

        //Album filter
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_ALBUM)
          {
            if (albumFilter < 0)
            {
              albumFilter = trackFilters.Count;
              trackFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              trackFilters[albumFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, trackFilters[albumFilter],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
        if (albumFilter >= 0)
        {
          SingleMediaItemAspect audioAspect;
          if (MediaItemAspect.TryGetAspect(extractedAspects, AudioAspect.Metadata, out audioAspect))
          {
            int? trackNumber = audioAspect.GetAttributeValue<int?>(AudioAspect.ATTR_TRACK);
            if (trackNumber.HasValue)
            {
              trackFilters[albumFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, trackFilters[albumFilter],
                new RelationalFilter(AudioAspect.ATTR_TRACK, RelationalOperator.EQ, trackNumber.Value));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, trackFilters.ToArray());
    }

    public static IFilter GetAlbumSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> albumFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(AudioAlbumAspect.ASPECT_ID))
        return null;

      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_ALBUM)
          {
            if (albumFilters.Count == 0)
            {
              albumFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              albumFilters[0] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, albumFilters[0],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, albumFilters.ToArray());
    }

    public static IFilter GetPersonSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> personFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(PersonAspect.ASPECT_ID))
        return null;

      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_PERSON)
          {
            if (personFilters.Count == 0)
            {
              personFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              personFilters[0] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, personFilters[0],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, personFilters.ToArray());
    }

    public static IFilter GetCompanySearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> companyFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(CompanyAspect.ASPECT_ID))
        return null;

      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_COMPANY)
          {
            if (companyFilters.Count == 0)
            {
              companyFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              companyFilters[0] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, companyFilters[0],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, companyFilters.ToArray());
    }

    public static void UpdatePersons(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<PersonInfo> infoPersons, bool forAlbum)
    {
      if (aspects.ContainsKey(TempPersonAspect.ASPECT_ID))
      {
        IList<MultipleMediaItemAspect> persons;
        if (MediaItemAspect.TryGetAspects(aspects, TempPersonAspect.Metadata, out persons))
        {
          foreach (MultipleMediaItemAspect person in persons)
          {
            if (person.GetAttributeValue<bool>(TempPersonAspect.ATTR_FROMALBUM) == forAlbum)
            {
              PersonInfo info = infoPersons.Find(p => p.Name.Equals(person.GetAttributeValue<string>(TempPersonAspect.ATTR_NAME), StringComparison.InvariantCultureIgnoreCase) &&
                  p.Occupation == person.GetAttributeValue<string>(TempPersonAspect.ATTR_OCCUPATION));
              if (info != null && string.IsNullOrEmpty(info.MusicBrainzId))
                info.MusicBrainzId = person.GetAttributeValue<string>(TempPersonAspect.ATTR_MBID);
            }
          }
        }
      }
    }

    public static void StorePersons(IDictionary<Guid, IList<MediaItemAspect>> aspects, List<PersonInfo> infoPersons, bool forAlbum)
    {
      foreach (PersonInfo person in infoPersons)
      {
        MultipleMediaItemAspect personAspect = MediaItemAspect.CreateAspect(aspects, TempPersonAspect.Metadata);
        personAspect.SetAttribute(TempPersonAspect.ATTR_MBID, person.MusicBrainzId);
        personAspect.SetAttribute(TempPersonAspect.ATTR_NAME, person.Name);
        personAspect.SetAttribute(TempPersonAspect.ATTR_OCCUPATION, person.Occupation);
        personAspect.SetAttribute(TempPersonAspect.ATTR_FROMALBUM, forAlbum);
      }
    }

    public static void StoreAlbum(IDictionary<Guid, IList<MediaItemAspect>> aspects, string albumName, string albumSortName)
    {
      SingleMediaItemAspect personAspect = MediaItemAspect.GetOrCreateAspect(aspects, TempAlbumAspect.Metadata);
      personAspect.SetAttribute(TempAlbumAspect.ATTR_NAME, albumName);
      personAspect.SetAttribute(TempAlbumAspect.ATTR_SORT_NAME, albumSortName);
    }

    public static void UpdateAlbum(IDictionary<Guid, IList<MediaItemAspect>> aspects, AlbumInfo album)
    {
      if (aspects.ContainsKey(TempAlbumAspect.ASPECT_ID))
      {
        SingleMediaItemAspect albumAspect;
        if (MediaItemAspect.TryGetAspect(aspects, TempAlbumAspect.Metadata, out albumAspect))
        {
          string sortName = albumAspect.GetAttributeValue<string>(TempAlbumAspect.ATTR_SORT_NAME);
          if (!string.IsNullOrEmpty(sortName))
            album.AlbumSort = sortName;
        }
      }
    }
  }
}
