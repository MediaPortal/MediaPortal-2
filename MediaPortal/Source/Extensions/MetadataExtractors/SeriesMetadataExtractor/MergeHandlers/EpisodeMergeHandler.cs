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
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  class EpisodeMergeHandler : IMediaMergeHandler
  {
    #region Constants

    private static readonly Guid[] MERGE_ASPECTS = { EpisodeAspect.ASPECT_ID };

    /// <summary>
    /// GUID string for the episode merge handler.
    /// </summary>
    public const string MERGEHANDLER_ID_STR = "62536C70-A9BB-4371-860E-83BE975E8DD4";

    /// <summary>
    /// Episode merge handler GUID.
    /// </summary>
    public static Guid MERGEHANDLER_ID = new Guid(MERGEHANDLER_ID_STR);

    #endregion

    protected MergeHandlerMetadata _metadata;

    public EpisodeMergeHandler()
    {
      _metadata = new MergeHandlerMetadata(MERGEHANDLER_ID, "Episode merge handler");
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
      get { return EpisodeInfo.EQUALITY_ASPECTS; }
    }

    public IFilter GetSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      return SeriesRelationshipExtractor.GetEpisodeSearchFilter(extractedAspects);
    }

    public bool TryMatch(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      if (!existingAspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        return false;

      EpisodeInfo linkedEpisode = new EpisodeInfo();
      if (!linkedEpisode.FromMetadata(extractedAspects))
        return false;

      EpisodeInfo existingEpisode = new EpisodeInfo();
      if (!existingEpisode.FromMetadata(existingAspects))
        return false;

      return linkedEpisode.Equals(existingEpisode);
    }

    public bool TryMerge(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      try
      {
        EpisodeInfo existing = new EpisodeInfo();
        EpisodeInfo extracted = new EpisodeInfo();

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
            //Don't allow merge of subtitles into virtual item
            if (extractedAspects.ContainsKey(SubtitleAspect.ASPECT_ID) && !extractedAspects.ContainsKey(VideoStreamAspect.ASPECT_ID))
              return false;

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

            existing.FromMetadata(existingAspects);
            extracted.FromMetadata(extractedAspects);

            existing.MergeWith(extracted, true);
            existing.SetMetadata(existingAspects);
            return true;
          }

          //Merge
          if (providerResourceAspects.Where(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB).Any())
          {
            if (providerResourceAspects.Where(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB).Any() ||
              existingProviderResourceAspects.Where(p => p.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_STUB).Any())
            {
              MediaItemAspect.SetAttribute(existingAspects, MediaAspect.ATTR_ISVIRTUAL, false);
              MediaItemAspect.SetAttribute(existingAspects, MediaAspect.ATTR_ISSTUB, true);
              if (!ResourceAspectMerger.MergeVideoResourceAspects(extractedAspects, existingAspects))
                return false;
            }
          }
        }

        existing.FromMetadata(existingAspects);
        extracted.FromMetadata(extractedAspects);

        existing.MergeWith(extracted, true);
        existing.SetMetadata(existingAspects);
        if (!ResourceAspectMerger.MergeVideoResourceAspects(extractedAspects, existingAspects))
          return false;

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
      //Don't allow subtitles for episode without actual episode being present
      if (extractedAspects.ContainsKey(SubtitleAspect.ASPECT_ID) && !extractedAspects.ContainsKey(VideoStreamAspect.ASPECT_ID))
        return true;

      //Allowed to be virtual so never requires merge
      return false;
    }
  }
}
