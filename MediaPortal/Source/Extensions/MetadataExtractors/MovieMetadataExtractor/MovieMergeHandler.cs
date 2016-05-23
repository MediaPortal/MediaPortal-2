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

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  class MovieMergeHandler : IMediaMergeHandler
  {
    #region Constants

    private static readonly Guid[] MERGE_ASPECTS = { MovieAspect.ASPECT_ID };

    /// <summary>
    /// GUID string for the movie merge handler.
    /// </summary>
    public const string MERGEHANDLER_ID_STR = "39F1994A-36E6-4638-8E55-2619023BE27D";

    /// <summary>
    /// Movie merge handler GUID.
    /// </summary>
    public static Guid MERGEHANDLER_ID = new Guid(MERGEHANDLER_ID_STR);

    #endregion

    protected MergeHandlerMetadata _metadata;

    public MovieMergeHandler()
    {
      _metadata = new MergeHandlerMetadata(MERGEHANDLER_ID, "Movie merge handler");
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
      if (!existingAspects.ContainsKey(MovieAspect.ASPECT_ID) || !existingAspects.ContainsKey(VideoAspect.ASPECT_ID))
        return false;

      return true;
    }

    public bool TryMerge(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      try
      {
        //Extracted aspects
        IList<MultipleMediaItemAspect> providerResourceAspects;
        if (!MediaItemAspect.TryGetAspects(extractedAspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
          return false;

        IList<MultipleMediaItemAspect> videoAspects;
        MediaItemAspect.TryGetAspects(extractedAspects, VideoAspect.Metadata, out videoAspects);

        IList<MultipleMediaItemAspect> videoAudioAspects;
        MediaItemAspect.TryGetAspects(extractedAspects, VideoAudioAspect.Metadata, out videoAudioAspects);

        IList<MultipleMediaItemAspect> subtitleAspects;
        MediaItemAspect.TryGetAspects(extractedAspects, SubtitleAspect.Metadata, out subtitleAspects);

        //Existing aspects
        IList<MultipleMediaItemAspect> existingProviderResourceAspects;
        MediaItemAspect.TryGetAspects(existingAspects, ProviderResourceAspect.Metadata, out existingProviderResourceAspects);

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

        string accessorPath;
        ResourcePath resourcePath;
        bool resourceExists = false; //Resource might already be added in the initial add
        foreach (MultipleMediaItemAspect providerResourceAspect in providerResourceAspects)
        {
          if (existingProviderResourceAspects != null)
          {
            accessorPath = (string)providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
            resourcePath = ResourcePath.Deserialize(accessorPath);
            string extractedPath = LocalFsResourceProviderBase.ToDosPath(resourcePath);

            foreach (MultipleMediaItemAspect exisitingProviderResourceAspect in existingProviderResourceAspects)
            {
              accessorPath = (string)exisitingProviderResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
              resourcePath = ResourcePath.Deserialize(accessorPath);
              string existingPath = LocalFsResourceProviderBase.ToDosPath(resourcePath);

              if (extractedPath.Equals(existingPath, StringComparison.InvariantCultureIgnoreCase))
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
          newPra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_TYPE, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_TYPE));
          newPra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_PART, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_PART));
          newPra.SetAttribute(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID));
          newPra.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_SYSTEM_ID));

          if (videoAspects != null)
          {
            foreach (MultipleMediaItemAspect videoAspect in videoAspects)
            {
              int videoResourceIndex = videoAspect.GetAttributeValue<int>(VideoAspect.ATTR_RESOURCE_INDEX);
              if (videoResourceIndex == resouceIndex)
              {
                MultipleMediaItemAspect newVa = MediaItemAspect.CreateAspect(existingAspects, VideoAspect.Metadata);
                newVa.SetAttribute(VideoAspect.ATTR_RESOURCE_INDEX, resourceIndexMap[videoResourceIndex]);
                newVa.SetAttribute(VideoAspect.ATTR_STREAM_INDEX, videoAspect.GetAttributeValue(VideoAspect.ATTR_STREAM_INDEX));
                newVa.SetAttribute(VideoAspect.ATTR_ASPECTRATIO, videoAspect.GetAttributeValue(VideoAspect.ATTR_ASPECTRATIO));
                newVa.SetAttribute(VideoAspect.ATTR_AUDIOSTREAMCOUNT, videoAspect.GetAttributeValue(VideoAspect.ATTR_AUDIOSTREAMCOUNT));
                newVa.SetAttribute(VideoAspect.ATTR_DURATION, videoAspect.GetAttributeValue(VideoAspect.ATTR_DURATION));
                newVa.SetAttribute(VideoAspect.ATTR_FPS, videoAspect.GetAttributeValue(VideoAspect.ATTR_FPS));
                newVa.SetAttribute(VideoAspect.ATTR_HEIGHT, videoAspect.GetAttributeValue(VideoAspect.ATTR_HEIGHT));
                newVa.SetAttribute(VideoAspect.ATTR_ISDVD, videoAspect.GetAttributeValue(VideoAspect.ATTR_ISDVD));
                newVa.SetAttribute(VideoAspect.ATTR_VIDEOBITRATE, videoAspect.GetAttributeValue(VideoAspect.ATTR_VIDEOBITRATE));
                newVa.SetAttribute(VideoAspect.ATTR_VIDEOENCODING, videoAspect.GetAttributeValue(VideoAspect.ATTR_VIDEOENCODING));
                newVa.SetAttribute(VideoAspect.ATTR_WIDTH, videoAspect.GetAttributeValue(VideoAspect.ATTR_WIDTH));
              }
            }
          }

          if (videoAudioAspects != null)
          {
            foreach (MultipleMediaItemAspect videoAudioAspect in videoAudioAspects)
            {
              int audioResourceIndex = videoAudioAspect.GetAttributeValue<int>(VideoAudioAspect.ATTR_RESOURCE_INDEX);
              if (audioResourceIndex == resouceIndex)
              {
                MultipleMediaItemAspect newVaa = MediaItemAspect.CreateAspect(existingAspects, VideoAudioAspect.Metadata);
                newVaa.SetAttribute(VideoAudioAspect.ATTR_RESOURCE_INDEX, resourceIndexMap[audioResourceIndex]);
                newVaa.SetAttribute(VideoAudioAspect.ATTR_STREAM_INDEX, videoAudioAspect.GetAttributeValue(VideoAudioAspect.ATTR_STREAM_INDEX));
                newVaa.SetAttribute(VideoAudioAspect.ATTR_AUDIOBITRATE, videoAudioAspect.GetAttributeValue(VideoAudioAspect.ATTR_AUDIOBITRATE));
                newVaa.SetAttribute(VideoAudioAspect.ATTR_AUDIOCHANNELS, videoAudioAspect.GetAttributeValue(VideoAudioAspect.ATTR_AUDIOCHANNELS));
                newVaa.SetAttribute(VideoAudioAspect.ATTR_AUDIOENCODING, videoAudioAspect.GetAttributeValue(VideoAudioAspect.ATTR_AUDIOENCODING));
                newVaa.SetAttribute(VideoAudioAspect.ATTR_AUDIOLANGUAGE, videoAudioAspect.GetAttributeValue(VideoAudioAspect.ATTR_AUDIOLANGUAGE));
              }
            }
          }

          //Internal subtitles
          if (subtitleAspects != null)
          {
            foreach (MultipleMediaItemAspect subAspect in subtitleAspects)
            {
              int videoResourceIndex = subAspect.GetAttributeValue<int>(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX);
              int subResourceIndex = subAspect.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX);
              if (videoResourceIndex == resouceIndex && subResourceIndex == -1)
              {
                MultipleMediaItemAspect newSa = MediaItemAspect.CreateAspect(existingAspects, SubtitleAspect.Metadata);
                newSa.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, resourceIndexMap[videoResourceIndex]);
                newSa.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, resourceIndexMap[videoResourceIndex]);
                newSa.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, subAspect.GetAttributeValue(SubtitleAspect.ATTR_STREAM_INDEX));
                newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_ENCODING, subAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_ENCODING));
                newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, subAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_FORMAT));
                newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE, subAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE));
                newSa.SetAttribute(SubtitleAspect.ATTR_DEFAULT, subAspect.GetAttributeValue(SubtitleAspect.ATTR_DEFAULT));
                newSa.SetAttribute(SubtitleAspect.ATTR_FORCED, subAspect.GetAttributeValue(SubtitleAspect.ATTR_FORCED));
              }
            }
          }

          //External subtitles
          if (subtitleAspects != null)
          {
            foreach (MultipleMediaItemAspect subAspect in subtitleAspects)
            {
              int subResourceIndex = subAspect.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX);
              if (subResourceIndex == resouceIndex)
              {
                //Find video resource
                int videoResourceIndex = -1;
                if (existingProviderResourceAspects != null)
                {
                  foreach (MultipleMediaItemAspect existingProviderResourceAspect in existingProviderResourceAspects)
                  {
                    accessorPath = (string)providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                    resourcePath = ResourcePath.Deserialize(accessorPath);
                    string subPath = LocalFsResourceProviderBase.ToDosPath(resourcePath);

                    accessorPath = (string)existingProviderResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                    resourcePath = ResourcePath.Deserialize(accessorPath);
                    string videoPath = LocalFsResourceProviderBase.ToDosPath(resourcePath);

                    if (Path.GetFileNameWithoutExtension(subPath).StartsWith(Path.GetFileNameWithoutExtension(videoPath), StringComparison.InvariantCultureIgnoreCase))
                    {
                      string resType = (string)existingProviderResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_TYPE);
                      if (resType.StartsWith("VIDEO", StringComparison.InvariantCultureIgnoreCase))
                      {
                        videoResourceIndex = providerResourceAspect.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
                        break;
                      }
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
        ServiceRegistration.Get<ILogger>().Info("MovieMergeHandler: Exception merging resources (Text: '{0}')", e.Message);
        return false;
      }
    }

    public bool RequiresMerge(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      if (extractedAspects.ContainsKey(VideoAspect.ASPECT_ID))
        return false;

      return true;
    }
  }
}
