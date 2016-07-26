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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using System.IO;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.Services.ResourceAccess.VirtualResourceProvider;

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
        //Extracted aspects
        IList<MultipleMediaItemAspect> providerResourceAspects;
        if (!MediaItemAspect.TryGetAspects(extractedAspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
          return false;

        //Don't merge virtual resource
        string accessorPath = (string)providerResourceAspects[0].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
        ResourcePath resourcePath = resourcePath = ResourcePath.Deserialize(accessorPath);
        if (resourcePath.BasePathSegment.ProviderId == VirtualResourceProvider.VIRTUAL_RESOURCE_PROVIDER_ID)
          return false;

        IList<MultipleMediaItemAspect> subtitleAspects;
        MediaItemAspect.TryGetAspects(extractedAspects, SubtitleAspect.Metadata, out subtitleAspects);

        //Existing aspects
        IList<MultipleMediaItemAspect> existingProviderResourceAspects;
        MediaItemAspect.TryGetAspects(existingAspects, ProviderResourceAspect.Metadata, out existingProviderResourceAspects);

        //Replace if existing is a virtual resource
        accessorPath = (string)existingProviderResourceAspects[0].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
        resourcePath = resourcePath = ResourcePath.Deserialize(accessorPath);
        if (resourcePath.BasePathSegment.ProviderId == VirtualResourceProvider.VIRTUAL_RESOURCE_PROVIDER_ID)
        {
          //Don't allow merge of subtitles into virtual item
          if (extractedAspects.ContainsKey(SubtitleAspect.ASPECT_ID) && !extractedAspects.ContainsKey(VideoStreamAspect.ASPECT_ID))
            return false;

          existingAspects[MediaAspect.ASPECT_ID][0].SetAttribute(MediaAspect.ATTR_ISVIRTUAL, false);
          existingAspects.Remove(ProviderResourceAspect.ASPECT_ID);
          foreach (Guid aspect in extractedAspects.Keys)
          {
            if (!existingAspects.ContainsKey(aspect))
              existingAspects.Add(aspect, extractedAspects[aspect]);
          }
          return true; 
        }

        //Merge
        Dictionary<int, int> resourceIndexMap = new Dictionary<int, int>();
        int newResourceIndex = -1;
        if (existingProviderResourceAspects != null)
        {
          foreach (MultipleMediaItemAspect providerResourceAspect in existingProviderResourceAspects)
          {
            int resouceIndex = providerResourceAspect.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
            if (newResourceIndex < resouceIndex)
            {
              newResourceIndex = resouceIndex;
            }
          }
        }
        newResourceIndex++;

        bool resourceExists = false; //Resource might already be added in the initial add
        foreach (MultipleMediaItemAspect providerResourceAspect in providerResourceAspects)
        {
          if (existingProviderResourceAspects != null)
          {
            accessorPath = (string)providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

            foreach (MultipleMediaItemAspect exisitingProviderResourceAspect in existingProviderResourceAspects)
            {
              string existingAccessorPath = (string)exisitingProviderResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
              if (accessorPath.Equals(existingAccessorPath, StringComparison.InvariantCultureIgnoreCase))
              {
                resourceExists = true;
                break;
              }
            }
          }

          if (resourceExists)
            continue;

          int resouceIndex = providerResourceAspect.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
          if (!resourceIndexMap.ContainsKey(resouceIndex))
            resourceIndexMap.Add(resouceIndex, newResourceIndex);
          newResourceIndex++;

          MultipleMediaItemAspect newPra = MediaItemAspect.CreateAspect(existingAspects, ProviderResourceAspect.Metadata);
          newPra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, resourceIndexMap[resouceIndex]);
          newPra.SetAttribute(ProviderResourceAspect.ATTR_PRIMARY, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_PRIMARY));
          newPra.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE));
          newPra.SetAttribute(ProviderResourceAspect.ATTR_SIZE, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_SIZE));
          newPra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
          newPra.SetAttribute(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID));
          newPra.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_SYSTEM_ID));

          //External subtitles
          ResourcePath existingResourcePath;
          if (subtitleAspects != null)
          {
            foreach (MultipleMediaItemAspect subAspect in subtitleAspects)
            {
              if (existingProviderResourceAspects == null)
                continue;

              int subResourceIndex = subAspect.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX);
              if (subResourceIndex == resouceIndex)
              {
                //Find video resource
                int videoResourceIndex = -1;
                foreach (MultipleMediaItemAspect existingProviderResourceAspect in existingProviderResourceAspects)
                {
                  accessorPath = (string)providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                  resourcePath = ResourcePath.Deserialize(accessorPath);

                  accessorPath = (string)existingProviderResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                  existingResourcePath = ResourcePath.Deserialize(accessorPath);

                  if (ResourcePath.GetFileNameWithoutExtension(resourcePath).StartsWith(ResourcePath.GetFileNameWithoutExtension(existingResourcePath), StringComparison.InvariantCultureIgnoreCase))
                  {
                    bool resPrimary = existingProviderResourceAspect.GetAttributeValue<bool>(ProviderResourceAspect.ATTR_PRIMARY);
                    if (resPrimary == true)
                    {
                      videoResourceIndex = providerResourceAspect.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
                      break;
                    }
                  }
                }

                MultipleMediaItemAspect newSa = MediaItemAspect.CreateAspect(existingAspects, SubtitleAspect.Metadata);
                newSa.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, videoResourceIndex);
                newSa.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, resourceIndexMap[subResourceIndex]);
                newSa.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, subAspect.GetAttributeValue(SubtitleAspect.ATTR_STREAM_INDEX));
                newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_ENCODING, subAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_ENCODING));
                newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, subAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_FORMAT));
                newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE, subAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE));
                newSa.SetAttribute(SubtitleAspect.ATTR_DEFAULT, subAspect.GetAttributeValue(SubtitleAspect.ATTR_DEFAULT));
                newSa.SetAttribute(SubtitleAspect.ATTR_FORCED, subAspect.GetAttributeValue(SubtitleAspect.ATTR_FORCED));
              }
            }
          }
        }

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
