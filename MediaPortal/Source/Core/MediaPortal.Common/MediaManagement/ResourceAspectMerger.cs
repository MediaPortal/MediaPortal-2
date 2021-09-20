#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

    /// <summary>
    /// Merges <see cref="ProviderResourceAspect"/>, <see cref="VideoStreamAspect"/>, <see cref="VideoAudioStreamAspect"/> and <see cref="SubtitleAspect"/> aspects
    /// contained in <paramref name="extractedAspects"/> into <paramref name="existingAspects"/> and updates the resource indices and set numbers to ensure they remain unique.
    /// </summary>
    /// <param name="extractedAspects">The new aspects to merge in.</param>
    /// <param name="existingAspects">The exisiting aspects to merge in to.</param>
    /// <returns><c>true</c> if the existing aspects were updated; else <c>false</c>.</returns>
    public static bool MergeVideoResourceAspects(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects)
    {
      //Extracted aspects
      IList<MultipleMediaItemAspect> providerResourceAspects;
      if (!MediaItemAspect.TryGetAspects(extractedAspects, ProviderResourceAspect.Metadata, out providerResourceAspects))
        return false;

      IList<MultipleMediaItemAspect> videoAspects;
      MediaItemAspect.TryGetAspects(extractedAspects, VideoStreamAspect.Metadata, out videoAspects);

      IList<MultipleMediaItemAspect> videoAudioAspects;
      MediaItemAspect.TryGetAspects(extractedAspects, VideoAudioStreamAspect.Metadata, out videoAudioAspects);

      IList<MultipleMediaItemAspect> subtitleAspects;
      MediaItemAspect.TryGetAspects(extractedAspects, SubtitleAspect.Metadata, out subtitleAspects);

      //Existing aspects
      IList<MultipleMediaItemAspect> existingProviderResourceAspects;
      MediaItemAspect.TryGetAspects(existingAspects, ProviderResourceAspect.Metadata, out existingProviderResourceAspects);

      // Get the maximum resource index in the existing aspects, new resource indices will start after this
      int newResourceIndex = existingProviderResourceAspects != null ? existingProviderResourceAspects.Max(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX)) : -1;

      // Loop through each new provider aspect, update it's resource index, add the updated aspect to the existing aspects, then add and update any other related aspects to point to this new index.
      // Ordered so that primary resources are added first so that the existing aspects will contain them before any secondary resources that link to them are added.
      foreach (MultipleMediaItemAspect providerResourceAspect in providerResourceAspects.OrderBy(p => p.GetAttributeValue<int?>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY ? 0 : 1))
      {
        newResourceIndex++;

        if (existingProviderResourceAspects != null)
        {
          // Check if this resource accessor already exists, if so skip it
          string accessorPath = providerResourceAspect.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
          if (existingProviderResourceAspects.Any(pra => accessorPath.Equals(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH))))
            continue;
        }

        // Update the resource index of the provider resource aspect and add it to the existing aspects
        AddProviderResourceAspectToExistingAspects(existingAspects, providerResourceAspect, newResourceIndex);
        // Get the updated list of provider resource aspects, needed for adding external subtitles and later iterations
        MediaItemAspect.TryGetAspects(existingAspects, ProviderResourceAspect.Metadata, out existingProviderResourceAspects);

        // Update the index on any video aspects that link to this provider aspect and add them to the existing aspects
        int resourceIndex = providerResourceAspect.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX);
        if (videoAspects != null)
          foreach (MultipleMediaItemAspect videoAspect in videoAspects.Where(v => v.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX) == resourceIndex))
            AddVideoAspectToExistingAspects(existingAspects, videoAspect, newResourceIndex);

        if (videoAudioAspects != null)
          foreach (MultipleMediaItemAspect videoAudioAspect in videoAudioAspects.Where(va => va.GetAttributeValue<int>(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX) == resourceIndex))
            AddVideoAudioAspectToExistingAspects(existingAspects, videoAudioAspect, newResourceIndex);

        // Two types of subtitles, internal that have the same resource index as the video, and external that link to a different video resource index.
        if (subtitleAspects != null)
        {
          //Internal subtitles, both resource and video index will point to the new index
          foreach (MultipleMediaItemAspect subAspect in subtitleAspects.Where(s => s.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX) == resourceIndex && s.GetAttributeValue<int>(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX) == resourceIndex))
            AddSubtitleAspectToExistingAspects(existingAspects, subAspect, newResourceIndex, newResourceIndex);

          //External subtitles, we need to match these to the video that has a filename that this subtitle's filename starts with to get the updated video resource index
          string subtitleFilenameWithoutExtension = ResourcePath.GetFileNameWithoutExtension(ResourcePath.Deserialize(providerResourceAspect.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)));

          foreach (MultipleMediaItemAspect subAspect in subtitleAspects.Where(s => s.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX) == resourceIndex && s.GetAttributeValue<int>(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX) != resourceIndex))
          {
            // Find the matching video resource for this external subtitle to get it's updated resource index, the video should be a primary resource so should already have been added to the
            // exisiting aspects by time we get to external subtitles (which are secondary) because we ordered the provider resource aspects by type before enumerating them.
            int? videoResourceIndex = existingProviderResourceAspects.FirstOrDefault(pra =>
              pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY &&
              subtitleFilenameWithoutExtension.StartsWith(ResourcePath.GetFileNameWithoutExtension(ResourcePath.Deserialize(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH))))
            )?
            .GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX);

            // Update the subtitles resource and video index and add it to the existing aspects
            if (videoResourceIndex.HasValue && videoResourceIndex >= 0)
              AddSubtitleAspectToExistingAspects(existingAspects, subAspect, newResourceIndex, videoResourceIndex.Value);
          }
        }
      }

      //Correct the set numbers so they are unique
      Dictionary<string, int> setList = new Dictionary<string, int>();
      IList<MultipleMediaItemAspect> existingVideoAspects;
      if (MediaItemAspect.TryGetAspects(existingAspects, VideoStreamAspect.Metadata, out existingVideoAspects))
      {
        int newMediaSet = 0;
        foreach (MultipleMediaItemAspect videoStreamAspect in existingVideoAspects)
        {
          string resourceAccessorPath = existingProviderResourceAspects.FirstOrDefault(pra =>
            pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) == videoStreamAspect.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX)
          )?
          .GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);

          if (resourceAccessorPath == null)
            continue;
          ResourcePath resourcePath = ResourcePath.Deserialize(resourceAccessorPath);
          string filename = resourcePath.FileName ?? Path.GetFileName(resourcePath.BasePathSegment.Path);
          videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET, GetMultipartSetNumber(ref setList, ref newMediaSet, filename));
        }
      }

      return true;
    }

    protected static void AddProviderResourceAspectToExistingAspects(IDictionary<Guid, IList<MediaItemAspect>> existingAspects, MultipleMediaItemAspect providerResourceAspect, int updatedResourceIndex)
    {
      MultipleMediaItemAspect newPra = MediaItemAspect.CreateAspect(existingAspects, ProviderResourceAspect.Metadata);
      newPra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, updatedResourceIndex);
      newPra.SetAttribute(ProviderResourceAspect.ATTR_TYPE, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_TYPE));
      newPra.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_MIME_TYPE));
      newPra.SetAttribute(ProviderResourceAspect.ATTR_SIZE, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_SIZE));
      newPra.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH));
      newPra.SetAttribute(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_PARENT_DIRECTORY_ID));
      newPra.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, providerResourceAspect.GetAttributeValue(ProviderResourceAspect.ATTR_SYSTEM_ID));
    }

    protected static void AddVideoAspectToExistingAspects(IDictionary<Guid, IList<MediaItemAspect>> existingAspects, MultipleMediaItemAspect videoAspect, int updatedResourceIndex)
    {
      MultipleMediaItemAspect newVa = MediaItemAspect.CreateAspect(existingAspects, VideoStreamAspect.Metadata);
      newVa.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, updatedResourceIndex);
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

    protected static void AddVideoAudioAspectToExistingAspects(IDictionary<Guid, IList<MediaItemAspect>> existingAspects, MultipleMediaItemAspect videoAudioAspect, int updatedResourceIndex)
    {
      MultipleMediaItemAspect newVaa = MediaItemAspect.CreateAspect(existingAspects, VideoAudioStreamAspect.Metadata);
      newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, updatedResourceIndex);
      newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, videoAudioAspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_STREAM_INDEX));
      newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOBITRATE, videoAudioAspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOBITRATE));
      newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, videoAudioAspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS));
      newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOENCODING, videoAudioAspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOENCODING));
      newVaa.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE, videoAudioAspect.GetAttributeValue(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE));
    }

    protected static void AddSubtitleAspectToExistingAspects(IDictionary<Guid, IList<MediaItemAspect>> existingAspects, MultipleMediaItemAspect subtitleAspect, int updatedResourceIndex, int updatedVideoResourceIndex)
    {
      MultipleMediaItemAspect newSa = MediaItemAspect.CreateAspect(existingAspects, SubtitleAspect.Metadata);
      newSa.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, updatedVideoResourceIndex);
      newSa.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, updatedResourceIndex);
      newSa.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, subtitleAspect.GetAttributeValue(SubtitleAspect.ATTR_STREAM_INDEX));
      newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_ENCODING, subtitleAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_ENCODING));
      newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, subtitleAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_FORMAT));
      newSa.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE, subtitleAspect.GetAttributeValue(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE));
      newSa.SetAttribute(SubtitleAspect.ATTR_INTERNAL, subtitleAspect.GetAttributeValue(SubtitleAspect.ATTR_INTERNAL));
      newSa.SetAttribute(SubtitleAspect.ATTR_DEFAULT, subtitleAspect.GetAttributeValue(SubtitleAspect.ATTR_DEFAULT));
      newSa.SetAttribute(SubtitleAspect.ATTR_FORCED, subtitleAspect.GetAttributeValue(SubtitleAspect.ATTR_FORCED));
    }
  }
}
