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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Services.ResourceAccess.VirtualResourceProvider;
using MediaPortal.Common.MediaManagement.MLQueries;
using System.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  class TrackMergeHandler : IMediaMergeHandler
  {
    #region Constants

    private static readonly Guid[] MERGE_ASPECTS = { AudioAspect.ASPECT_ID };

    /// <summary>
    /// GUID string for the track merge handler.
    /// </summary>
    public const string MERGEHANDLER_ID_STR = "5266897F-0251-4927-80BA-66DCB1AB52BE";

    /// <summary>
    /// Track merge handler GUID.
    /// </summary>
    public static Guid MERGEHANDLER_ID = new Guid(MERGEHANDLER_ID_STR);

    #endregion

    protected MergeHandlerMetadata _metadata;

    public TrackMergeHandler()
    {
      _metadata = new MergeHandlerMetadata(MERGEHANDLER_ID, "Track merge handler");
    }

    public Guid[] MergeableAspects
    {
      get
      {
        return MERGE_ASPECTS;
      }
    }

    public MergeHandlerMetadata Metadata
    {
      get { return _metadata; }
    }

    public Guid[] MatchAspects
    {
      get { return TrackInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      SingleMediaItemAspect audioAspect;
      if (!MediaItemAspect.TryGetAspect(extractedAspects, AudioAspect.Metadata, out audioAspect))
        return null;

      IFilter trackFilter = RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_TRACK);
      IFilter albumFilter = RelationshipExtractorUtils.CreateExternalItemFilter(extractedAspects, ExternalIdentifierAspect.TYPE_ALBUM);
      if (albumFilter == null)
        return trackFilter;

      int? trackNumber = audioAspect.GetAttributeValue<int?>(AudioAspect.ATTR_TRACK);
      if (trackNumber.HasValue)
        albumFilter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, albumFilter,
          new RelationalFilter(AudioAspect.ATTR_TRACK, RelationalOperator.EQ, trackNumber.Value));

      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, trackFilter, albumFilter);
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(AudioAspect.ASPECT_ID))
        return false;

      SingleMediaItemAspect extractedMediaAspect;
      if (!MediaItemAspect.TryGetAspect(extractedAspects, MediaAspect.Metadata, out extractedMediaAspect))
        return false;

      SingleMediaItemAspect existingMediaAspect;
      if (!MediaItemAspect.TryGetAspect(existingAspects, MediaAspect.Metadata, out existingMediaAspect))
        return false;

      //Only merge with a stub
      if (!extractedMediaAspect.GetAttributeValue<bool>(MediaAspect.ATTR_ISSTUB) && !existingMediaAspect.GetAttributeValue<bool>(MediaAspect.ATTR_ISSTUB))
        return false;

      TrackInfo linkedTrack = new TrackInfo();
      if (!linkedTrack.FromMetadata(extractedAspects))
        return false;

      TrackInfo existingTrack = new TrackInfo();
      if (!existingTrack.FromMetadata(existingAspects))
        return false;

      return linkedTrack.Equals(existingTrack);
    }

    public bool TryMerge(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      try
      {
        TrackInfo existing = new TrackInfo();
        TrackInfo extracted = new TrackInfo();

        //Extracted aspects
        IList<MultipleMediaItemAspect> providerResourceAspects;
        if (!MediaItemAspect.TryGetAspects(extractedAspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
          return false;

        //Existing aspects
        IList<MultipleMediaItemAspect> existingProviderResourceAspects;
        MediaItemAspect.TryGetAspects(existingAspects, ProviderResourceAspect.Metadata, out existingProviderResourceAspects);

        //Don't merge virtual resources
        if (!providerResourceAspects.Where(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_VIRTUAL).Any())
        {
          //Replace if existing is a virtual resource
          if (existingProviderResourceAspects.Where(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_VIRTUAL).Any())
          {
            MediaItemAspect.SetAttribute(existingAspects, MediaAspect.ATTR_ISVIRTUAL, false);
            MediaItemAspect.SetAttribute(existingAspects, MediaAspect.ATTR_ISSTUB, 
              providerResourceAspects.Where(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB).Any());
            var now = DateTime.Now;
            MediaItemAspect.SetAttribute(existingAspects, ImporterAspect.ATTR_DATEADDED, now);
            MediaItemAspect.SetAttribute(existingAspects, ImporterAspect.ATTR_LAST_IMPORT_DATE, now);
            existingAspects.Remove(ProviderResourceAspect.ASPECT_ID);
            foreach (Guid aspect in extractedAspects.Keys)
            {
              if (!existingAspects.ContainsKey(aspect))
                existingAspects.Add(aspect, extractedAspects[aspect]);
            }
          }
          else if (existingProviderResourceAspects.Where(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB).Any() ||
            providerResourceAspects.Where(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB).Any())
          {
            MediaItemAspect.SetAttribute(existingAspects, MediaAspect.ATTR_ISVIRTUAL, false);
            MediaItemAspect.SetAttribute(existingAspects, MediaAspect.ATTR_ISSTUB, true);
            foreach (Guid aspect in extractedAspects.Keys)
            {
              if (!existingAspects.ContainsKey(aspect))
                existingAspects.Add(aspect, extractedAspects[aspect]);
              else if (aspect == ProviderResourceAspect.ASPECT_ID)
              {
                int newResIndex = 0;
                foreach (MediaItemAspect mia in existingAspects[aspect])
                {
                  if(newResIndex <= mia.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX))
                    newResIndex = mia.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) + 1;
                }
                foreach (MediaItemAspect mia in extractedAspects[aspect])
                {
                  mia.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, newResIndex);
                  existingAspects[aspect].Add(mia);
                  newResIndex++;
                }
              }
            }
          }
        }

        existing.FromMetadata(existingAspects);
        extracted.FromMetadata(extractedAspects);

        existing.MergeWith(extracted, false, false);
        existing.SetMetadata(existingAspects);

        return true;
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        ServiceRegistration.Get<ILogger>().Info("EpisodeMergeHandler: Exception merging resources (Text: '{0}')", e.Message);
        return false;
      }
    }

    public bool RequiresMerge(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      //Allowed to be virtual so never requires merge
      return false;
    }
  }
}
