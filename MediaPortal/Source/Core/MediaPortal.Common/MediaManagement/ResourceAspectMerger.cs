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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaPortal.Common.MediaManagement
{
  public class ResourceAspectMerger
  {
    private static readonly int[] SUPPORTED_PART_NUMS = new int[] { 1, 2, 3, 4, 5 }; 

    private static int GetMultipartSetNumber(ref Dictionary<string, int> setList, ref int setNumber, string name)
    {
      //Remove part number so different part can be compared
      name = SUPPORTED_PART_NUMS.Aggregate(name, (current, replacement) => current.Replace(replacement.ToString(), "0"));
      name = name.ToUpperInvariant();

      if (setList.ContainsKey(name))
        return setList[name];

      setList.Add(name, setNumber);
      setNumber++;
      return setList[name];
    }

    public static bool MergeVideoResourceAspects(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      //Extracted aspects
      IList<MultipleMediaItemAspect> providerResourceAspects;
      if (!MediaItemAspect.TryGetAspects(extractedAspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
        return false;

      string accessorPath = (string)providerResourceAspects.First().GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      ResourcePath resourcePath = ResourcePath.Deserialize(accessorPath);

      IList<MultipleMediaItemAspect> videoAspects;
      MediaItemAspect.TryGetAspects(extractedAspects, VideoStreamAspect.Metadata, out videoAspects);

      IList<MultipleMediaItemAspect> videoAudioAspects;
      MediaItemAspect.TryGetAspects(extractedAspects, VideoAudioStreamAspect.Metadata, out videoAudioAspects);

      IList<MultipleMediaItemAspect> subtitleAspects;
      MediaItemAspect.TryGetAspects(extractedAspects, SubtitleAspect.Metadata, out subtitleAspects);

      //Existing aspects
      IList<MultipleMediaItemAspect> existingProviderResourceAspects;
      MediaItemAspect.TryGetAspects(existingAspects, ProviderResourceAspect.Metadata, out existingProviderResourceAspects);

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
        newPra.SetAttribute(ProviderResourceAspect.ATTR_TYPE, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_TYPE));
        newPra.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE));
        newPra.SetAttribute(ProviderResourceAspect.ATTR_SIZE, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_SIZE));
        newPra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
        newPra.SetAttribute(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID));
        newPra.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_SYSTEM_ID));

        if (videoAspects != null)
        {
          foreach (MultipleMediaItemAspect videoAspect in videoAspects)
          {
            int videoResourceIndex = videoAspect.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX);
            if (videoResourceIndex == resouceIndex)
            {
              MultipleMediaItemAspect newVa = MediaItemAspect.CreateAspect(existingAspects, VideoStreamAspect.Metadata);
              newVa.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, resourceIndexMap[videoResourceIndex]);
              newVa.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_STREAM_INDEX));
              newVa.SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_ASPECTRATIO));
              newVa.SetAttribute(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_AUDIOSTREAMCOUNT));
              newVa.SetAttribute(VideoStreamAspect.ATTR_DURATION, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_DURATION));
              newVa.SetAttribute(VideoStreamAspect.ATTR_FPS, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_FPS));
              newVa.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_HEIGHT));
              newVa.SetAttribute(VideoStreamAspect.ATTR_VIDEOBITRATE, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_VIDEOBITRATE));
              newVa.SetAttribute(VideoStreamAspect.ATTR_VIDEOENCODING, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_VIDEOENCODING));
              newVa.SetAttribute(VideoStreamAspect.ATTR_WIDTH, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_WIDTH));
              newVa.SetAttribute(VideoStreamAspect.ATTR_VIDEO_TYPE, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_VIDEO_TYPE));
              newVa.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_VIDEO_PART));
              newVa.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_VIDEO_PART_SET));
              newVa.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET_NAME, videoAspect.GetAttributeValue(VideoStreamAspect.ATTR_VIDEO_PART_SET_NAME));
            }
          }
        }

        //Correct sets
        Dictionary<string, int> setList = new Dictionary<string, int>();
        MediaItemAspect.TryGetAspects(existingAspects, ProviderResourceAspect.Metadata, out existingProviderResourceAspects);
        IList<MultipleMediaItemAspect> existingVideoAspects;
        if (MediaItemAspect.TryGetAspects(existingAspects, VideoStreamAspect.Metadata, out existingVideoAspects))
        {
          int newMediaSet = 0;
          Dictionary<int, int> knownSets = new Dictionary<int, int>();
          foreach (MultipleMediaItemAspect videoStreamAspect in existingVideoAspects)
          {
            string filename = null;
            foreach (MultipleMediaItemAspect pra in existingProviderResourceAspects)
            {
              if (pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) == videoStreamAspect.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX))
              {
                accessorPath = (string)pra.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                resourcePath = resourcePath = ResourcePath.Deserialize(accessorPath);
                filename = resourcePath.FileName ?? Path.GetFileName(resourcePath.BasePathSegment.Path);
                break;
              }
            }
            videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET, GetMultipartSetNumber(ref setList, ref newMediaSet, filename));
          }
        }

        if (videoAudioAspects != null)
        {
          foreach (MultipleMediaItemAspect videoAudioAspect in videoAudioAspects)
          {
            int audioResourceIndex = videoAudioAspect.GetAttributeValue<int>(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX);
            if (audioResourceIndex == resouceIndex)
            {
              MultipleMediaItemAspect newVaa = MediaItemAspect.CreateAspect(existingAspects, VideoAudioStreamAspect.Metadata);
              newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, resourceIndexMap[audioResourceIndex]);
              newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, videoAudioAspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_STREAM_INDEX));
              newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOBITRATE, videoAudioAspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOBITRATE));
              newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, videoAudioAspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS));
              newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOENCODING, videoAudioAspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOENCODING));
              newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE, videoAudioAspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE));
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
              newSa.SetAttribute(SubtitleAspect.ATTR_INTERNAL, subAspect.GetAttributeValue(SubtitleAspect.ATTR_INTERNAL));
              newSa.SetAttribute(SubtitleAspect.ATTR_DEFAULT, subAspect.GetAttributeValue(SubtitleAspect.ATTR_DEFAULT));
              newSa.SetAttribute(SubtitleAspect.ATTR_FORCED, subAspect.GetAttributeValue(SubtitleAspect.ATTR_FORCED));
            }
          }
        }

        //External subtitles
        ResourcePath existingResourcePath;
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

                  accessorPath = (string)existingProviderResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
                  existingResourcePath = ResourcePath.Deserialize(accessorPath);

                  if (ResourcePath.GetFileNameWithoutExtension(resourcePath).StartsWith(ResourcePath.GetFileNameWithoutExtension(existingResourcePath), StringComparison.InvariantCultureIgnoreCase))
                  {
                    bool resPrimary = existingProviderResourceAspect.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY;
                    if (resPrimary == true)
                    {
                      videoResourceIndex = providerResourceAspect.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
                      break;
                    }
                  }
                }
              }

              if (videoResourceIndex >= 0)
              {
                MultipleMediaItemAspect newSa = MediaItemAspect.CreateAspect(existingAspects, SubtitleAspect.Metadata);
                newSa.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, videoResourceIndex);
                newSa.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, resourceIndexMap[subResourceIndex]);
                newSa.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, subAspect.GetAttributeValue(SubtitleAspect.ATTR_STREAM_INDEX));
                newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_ENCODING, subAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_ENCODING));
                newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, subAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_FORMAT));
                newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE, subAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE));
                newSa.SetAttribute(SubtitleAspect.ATTR_INTERNAL, subAspect.GetAttributeValue(SubtitleAspect.ATTR_INTERNAL));
                newSa.SetAttribute(SubtitleAspect.ATTR_DEFAULT, subAspect.GetAttributeValue(SubtitleAspect.ATTR_DEFAULT));
                newSa.SetAttribute(SubtitleAspect.ATTR_FORCED, subAspect.GetAttributeValue(SubtitleAspect.ATTR_FORCED));
              }
            }
          }
        }
      }
      return true;
    }
  }
}
