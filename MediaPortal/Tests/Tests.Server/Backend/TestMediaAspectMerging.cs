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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Tests.Server.Backend
{
  public class TestMediaAspectMerging
  {
    #region Mock aspects creation

    public static IDictionary<Guid, IList<MediaItemAspect>> CreateMockAspects(string resourcePath, int videoCount, int initialVideoPart, int internalSubtitleCountPerVideo, int externalSubtitleCountPerVideo)
    {
      Dictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();

      int resourceIndex = 0;
      int currentVideoPart = initialVideoPart;
      for (int i = 0; i < videoCount; i++)
      {
        MediaItemAspect.GetOrCreateAspect(aspects, VideoAspect.Metadata);

        MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(aspects, ProviderResourceAspect.Metadata);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, resourceIndex);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_PRIMARY);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "video/unknown");
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, 100L);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, LocalFsResourceProviderBase.ToResourcePath(resourcePath + " part" + currentVideoPart++ + ".mp4").Serialize());

        MultipleMediaItemAspect videoStreamAspect = MediaItemAspect.CreateAspect(aspects, VideoStreamAspect.Metadata);
        videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_RESOURCE_INDEX, resourceIndex);
        videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_STREAM_INDEX, 0);
        videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_VIDEO_PART_SET, 0);
        videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_DURATION, 3600L);
        videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_WIDTH, 1920);
        videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_HEIGHT, 1080);
        videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_FPS, 25f);
        videoStreamAspect.SetAttribute(VideoStreamAspect.ATTR_ASPECTRATIO, 1.78f);

        MultipleMediaItemAspect videoAudioStreamAspect = MediaItemAspect.CreateAspect(aspects, VideoAudioStreamAspect.Metadata);
        videoAudioStreamAspect.SetAttribute(VideoAudioStreamAspect.ATTR_RESOURCE_INDEX, resourceIndex);
        videoAudioStreamAspect.SetAttribute(VideoAudioStreamAspect.ATTR_STREAM_INDEX, 1);
        videoAudioStreamAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOSAMPLERATE, 44100L);
        videoAudioStreamAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOBITRATE, 192L);
        videoAudioStreamAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOCHANNELS, 2);
        videoAudioStreamAspect.SetAttribute(VideoAudioStreamAspect.ATTR_AUDIOLANGUAGE, "eng");

        for (int j = 0; j < internalSubtitleCountPerVideo; j++)
        {
          MultipleMediaItemAspect subtitleAspect = MediaItemAspect.CreateAspect(aspects, SubtitleAspect.Metadata);
          subtitleAspect.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, resourceIndex);
          subtitleAspect.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, resourceIndex);
          subtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, SubtitleAspect.FORMAT_SRT);
          subtitleAspect.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, 2);
        }
        resourceIndex++;
      }
      
      for (int i = 0; i < externalSubtitleCountPerVideo; i++)
      {
        currentVideoPart = initialVideoPart;
        for (int j = 0; j < (videoCount < 1 ? 1 : videoCount); j++)
        {
          resourceIndex++;
          MultipleMediaItemAspect providerResourceAspect = MediaItemAspect.CreateAspect(aspects, ProviderResourceAspect.Metadata);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_INDEX, resourceIndex);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_TYPE, ProviderResourceAspect.TYPE_SECONDARY);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_MIME_TYPE, "text/srt");
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SIZE, 100L);
          providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, LocalFsResourceProviderBase.ToResourcePath(resourcePath + " part" + currentVideoPart++ + ".srt").Serialize());

          MultipleMediaItemAspect externalSubtitleAspect = MediaItemAspect.CreateAspect(aspects, SubtitleAspect.Metadata);
          externalSubtitleAspect.SetAttribute(SubtitleAspect.ATTR_RESOURCE_INDEX, resourceIndex);
          externalSubtitleAspect.SetAttribute(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX, j);
          externalSubtitleAspect.SetAttribute(SubtitleAspect.ATTR_SUBTITLE_FORMAT, SubtitleAspect.FORMAT_SRT);
          externalSubtitleAspect.SetAttribute(SubtitleAspect.ATTR_STREAM_INDEX, -1);
        }
      }

      return aspects;
    }

    #endregion

    #region Resource index merging tests

    public static IEnumerable ResourceIndicesTestCases
    {
      get
      {
        // Merge external subtitle into an existing resource
        var extractedAspects = CreateMockAspects("c:\\test", 0, 0, 0, 1);
        var existingAspects = CreateMockAspects("c:\\test", 1, 0, 0, 0);
        // Expecting 2 provider sources, 1 for video and 1 for external subtitle, and 1 video and 1 subtitle aspect 
        yield return new TestCaseData(extractedAspects, existingAspects, 2, 1, 0, 1);

        // Merge 2 video parts together
        extractedAspects = CreateMockAspects("c:\\test", 1, 1, 0, 0);
        existingAspects = CreateMockAspects("c:\\test", 1, 0, 0, 0);
        // Expecting 2 provider sources, 1 for each video and 2 video aspects
        yield return new TestCaseData(extractedAspects, existingAspects, 2, 2, 0, 0);

        // Merge 2 video parts, with internal and external subtitles, together
        extractedAspects = CreateMockAspects("c:\\test", 1, 1, 1, 1);
        existingAspects = CreateMockAspects("c:\\test", 1, 0, 1, 1);
        // Expecting 4 provider sources, 2 for each video and 2 for each external subtitle, and 2 of each video, internal subtitle and external subtitle aspects
        yield return new TestCaseData(extractedAspects, existingAspects, 4, 2, 2, 2);

        // Merge 2 sets, each made up of 2 parts (so 4 parts in total), each part has 1 internal and 1 external subtitle 
        extractedAspects = CreateMockAspects("c:\\test_y", 2, 0, 1, 1);
        existingAspects = CreateMockAspects("c:\\test_x", 2, 0, 1, 1);
        // 4 parts, each with a provider source for both a video and external subtitle, so 8 provider aspects in total, plus 4 of each video, internal subtitle and external subtitle aspects (1 for each part)
        yield return new TestCaseData(extractedAspects, existingAspects, 8, 4, 4, 4);
      }
    }

    [TestCaseSource(nameof(ResourceIndicesTestCases))]
    public void AreResourceIndicesUpdated(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects, int expectedProviderCount, int expectedVideoCount, int expectedInternalSubtitleCount, int expectedExternalSubtitleCount)
    {
      bool result = ResourceAspectMerger.MergeVideoResourceAspects(extractedAspects, existingAspects);

      Assert.IsTrue(result, "MergeVideoResourceAspects unexpectedly returned false");

      var providerResourceIndices = existingAspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out var pras) ? pras.Select(a => a.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX)).Distinct() : new int[0];
      var videoResourceIndices = existingAspects.TryGetValue(VideoStreamAspect.ASPECT_ID, out var vsas) ? vsas.Select(a => a.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX)) : new int[0];

      var internalSubtitleResourceIndices = existingAspects.TryGetValue(SubtitleAspect.ASPECT_ID, out var isas) ? isas
        .Where(a => a.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX) == a.GetAttributeValue<int>(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX))
        .Select(a => a.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX)) : new int[0];

      var externalSubtitleResourceIndices = existingAspects.TryGetValue(SubtitleAspect.ASPECT_ID, out var esas) ? esas
        .Where(a => a.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX) != a.GetAttributeValue<int>(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX))
        .Select(a => a.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX)) : new int[0];

      var externalSubtitleVideoIndices = existingAspects.TryGetValue(SubtitleAspect.ASPECT_ID, out var esvas) ? esvas
        .Where(a => a.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX) != a.GetAttributeValue<int>(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX))
        .Select(a => a.GetAttributeValue<int>(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX)) : new int[0];

      Assert.AreEqual(expectedProviderCount, providerResourceIndices.Count(), "Unexpected number of provider resource indices");
      Assert.IsTrue(providerResourceIndices.All(i => videoResourceIndices.Contains(i) || externalSubtitleResourceIndices.Contains(i)), "Not all provider sources have a video or external subtitle source");

      Assert.AreEqual(expectedVideoCount, videoResourceIndices.Count(), "Unexpected number of video resource indices");
      Assert.IsTrue(videoResourceIndices.All(i => providerResourceIndices.Contains(i)), "Not all videos have a provider resource");

      Assert.AreEqual(expectedInternalSubtitleCount, internalSubtitleResourceIndices.Count(), "Unexpected number of internal subtitle resource indices");
      Assert.IsTrue(internalSubtitleResourceIndices.All(i => providerResourceIndices.Contains(i)), "Not all internal subtitles have a provider resource");
      Assert.IsTrue(internalSubtitleResourceIndices.All(i => videoResourceIndices.Contains(i)), "Not all internal subtitles have a video resource");
      
      Assert.AreEqual(expectedExternalSubtitleCount, externalSubtitleResourceIndices.Count(), "Unexpected number of external subtitle resource indices");
      Assert.IsTrue(externalSubtitleResourceIndices.All(i => providerResourceIndices.Contains(i)), "Not all external subtitles have a provider resource");
      Assert.IsTrue(externalSubtitleVideoIndices.All(i => videoResourceIndices.Contains(i)), "Not all external subtitles have a video resource");

      Assert.AreEqual(0, videoResourceIndices.Distinct().Intersect(externalSubtitleResourceIndices.Distinct()).Count(), "Some external subtitles unexpectedly have the same resource index as their parent video");
    }

    #endregion

    #region Set number merging tests

    public static IEnumerable SetNumberTestCases
    {
      get
      {
        // Merge 2 sets, each made up of 2 parts
        var extractedAspects = CreateMockAspects("c:\\test_y", 2, 0, 0, 0);
        var existingAspects = CreateMockAspects("c:\\test_x", 2, 0, 0, 0);
        // 2 parts in each, with names starting with the given filename
        yield return new TestCaseData(extractedAspects, existingAspects, 2, "test_x", 2, "test_y");

        // Merge 2 parts to create 1 set
        extractedAspects = CreateMockAspects("c:\\test_x", 1, 1, 0, 0);
        existingAspects = CreateMockAspects("c:\\test_x", 1, 0, 0, 0);
        // 2 parts to create 1 set
        yield return new TestCaseData(extractedAspects, existingAspects, 2, "test_x", 0, null);
      }
    }

    [TestCaseSource(nameof(SetNumberTestCases))]
    public void AreSetNumbersUpdated(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects, IDictionary<Guid, IList<MediaItemAspect>> existingAspects, int expectedSet1Parts, string expectedSet1Filenames, int expectedSet2Parts, string expectedSet2Filenames)
    {
      bool result = ResourceAspectMerger.MergeVideoResourceAspects(extractedAspects, existingAspects);

      Assert.IsTrue(result, "MergeVideoResourceAspects unexpectedly returned false");

      if (!existingAspects.TryGetValue(VideoStreamAspect.ASPECT_ID, out var videoStreamAspects))
        videoStreamAspects = new List<MediaItemAspect>();

      if (!existingAspects.TryGetValue(ProviderResourceAspect.ASPECT_ID, out var providerResourceAspects))
        providerResourceAspects = new List<MediaItemAspect>();

      var set1ResourcePaths = videoStreamAspects.Where(a => a.GetAttributeValue<int?>(VideoStreamAspect.ATTR_VIDEO_PART_SET) == 0)
        .SelectMany(a => providerResourceAspects.Where(pa => pa.GetAttributeValue<int?>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) == a.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX)))
        .Select(a => ResourcePath.Deserialize(a.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)));

      var set2ResourcePaths = videoStreamAspects.Where(a => a.GetAttributeValue<int?>(VideoStreamAspect.ATTR_VIDEO_PART_SET) == 1)
        .SelectMany(a => providerResourceAspects.Where(pa => pa.GetAttributeValue<int?>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) == a.GetAttributeValue<int>(VideoStreamAspect.ATTR_RESOURCE_INDEX)))
        .Select(a => ResourcePath.Deserialize(a.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH)));

      Assert.AreEqual(expectedSet1Parts, set1ResourcePaths.Count(), "Unexpected number of parts in set 1");
      Assert.IsTrue(set1ResourcePaths.All(p => p.FileName.StartsWith(expectedSet1Filenames)), "Unexpected files in set 1");

      Assert.AreEqual(expectedSet2Parts, set2ResourcePaths.Count(), "Unexpected number of parts in set 2");
      Assert.IsTrue(set2ResourcePaths.All(p => p.FileName.StartsWith(expectedSet2Filenames)), "Unexpected files in set 2");
    }

    #endregion
  }
}
