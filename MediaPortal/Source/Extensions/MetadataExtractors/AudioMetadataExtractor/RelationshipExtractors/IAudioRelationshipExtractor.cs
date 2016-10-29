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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  public abstract class IAudioRelationshipExtractor
  {
    protected static MemoryCache _artistMemoryCache = null;
    protected static MemoryCache _composerMemoryCache = null;
    protected static MemoryCache _albumMemoryCache = null;
    protected static MemoryCache _labelMemoryCache = null;
    protected static CacheItemPolicy _cachePolicy = null;

    protected MemoryCache _checkMemoryCache = null;

    static IAudioRelationshipExtractor()
    {
      _artistMemoryCache = new MemoryCache("AudioArtistItemCache");
      _composerMemoryCache = new MemoryCache("AudioComposerItemCache");
      _albumMemoryCache = new MemoryCache("AudioAlbumItemCache");
      _labelMemoryCache = new MemoryCache("AudioLabelItemCache");

      _cachePolicy = new CacheItemPolicy();
      _cachePolicy.SlidingExpiration = TimeSpan.FromHours(AudioMetadataExtractor.MINIMUM_HOUR_AGE_BEFORE_UPDATE);
    }

    public virtual void ClearCache()
    {
      if (_artistMemoryCache != null)
        _artistMemoryCache.Trim(100);
      if (_composerMemoryCache != null)
        _composerMemoryCache.Trim(100);
      if (_albumMemoryCache != null)
        _albumMemoryCache.Trim(100);
      if (_labelMemoryCache != null)
        _labelMemoryCache.Trim(100);

      if (_checkMemoryCache != null)
        _checkMemoryCache.Trim(100);
    }

    protected virtual bool AddToArtistCache(Guid mediaItemId, PersonInfo mediaItemInfo)
    {
      if (_artistMemoryCache == null)
        return false;

      return _artistMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromArtistCache(PersonInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_artistMemoryCache == null)
        return false;

      List<string> items = _artistMemoryCache.Where(mi => mediaItemInfo.Equals((PersonInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _artistMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual bool AddToComposerCache(Guid mediaItemId, PersonInfo mediaItemInfo)
    {
      if (_composerMemoryCache == null)
        return false;

      return _composerMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromComposerCache(PersonInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_composerMemoryCache == null)
        return false;

      List<string> items = _composerMemoryCache.Where(mi => mediaItemInfo.Equals((PersonInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _composerMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual bool AddToAlbumCache(Guid mediaItemId, AlbumInfo mediaItemInfo)
    {
      if (_albumMemoryCache == null)
        return false;

      return _albumMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromAlbumCache(AlbumInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_albumMemoryCache == null)
        return false;

      List<string> items = _albumMemoryCache.Where(mi => mediaItemInfo.Equals((AlbumInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _albumMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual AlbumInfo GetFromAlbumCache(Guid mediaItemId)
    {
      if (_albumMemoryCache == null)
        return null;

      return (AlbumInfo)_albumMemoryCache[mediaItemId.ToString()];
    }

    protected virtual bool AddToLabelCache(Guid mediaItemId, CompanyInfo mediaItemInfo)
    {
      if (_labelMemoryCache == null)
        return false;

      return _labelMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromLabelCache(CompanyInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_labelMemoryCache == null)
        return false;

      List<string> items = _labelMemoryCache.Where(mi => mediaItemInfo.Equals((CompanyInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _labelMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual bool AddToCheckCache<T>(T mediaItemInfo)
    {
      if (_checkMemoryCache == null)
        _checkMemoryCache = new MemoryCache(GetType().ToString() + "CheckCache");

      List<string> items = _checkMemoryCache.Where(mi => mediaItemInfo.Equals((T)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
        return _checkMemoryCache.Add(items[0], mediaItemInfo, _cachePolicy);

      return _checkMemoryCache.Add(mediaItemInfo.ToString(), mediaItemInfo, _cachePolicy);
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
  }
}
