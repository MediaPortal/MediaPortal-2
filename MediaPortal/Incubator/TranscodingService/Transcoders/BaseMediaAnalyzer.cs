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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Extensions.TranscodingService.Interfaces.SlimTv;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Common.PathManager;
using Newtonsoft.Json;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using MediaPortal.Extensions.TranscodingService.Interfaces.MetaData;
using System.Threading.Tasks;
using MediaPortal.Common.Services.ResourceAccess;
using MediaPortal.Extensions.TranscodingService.Interfaces.Metadata.Streams;
using MediaPortal.Extensions.TranscodingService.Interfaces.Helpers;

namespace MediaPortal.Extensions.TranscodingService.Service.Transcoders
{
  public abstract class BaseMediaAnalyzer : IMediaAnalyzer
  {
    #region Constants

    protected static readonly string DEFAULT_ANALYSIS_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\Transcoding\");
    protected static readonly IEnumerable<string> OPTICAL_DISC_FILE_EXT = new string[] { ".vob", ".m2ts" };
    protected static string AUDIO_CATEGORY = "Audio";
    protected static string VIDEO_CATEGORY = "Video";
    protected static string IMAGE_CATEGORY = "Image";

    #endregion

    protected int _analyzerMaximumThreads;
    protected int _analyzerTimeout;
    protected long _analyzerStreamTimeout;
    protected ILogger _logger = null;
    protected readonly Dictionary<float, long> _h264MaxDpbMbs = new Dictionary<float, long>();
    protected SlimTvHandler _slimTvHandler = new SlimTvHandler();
    protected ICollection<string> _audioExtensions;
    protected ICollection<string> _videoExtensions;
    protected ICollection<string> _imageExtensions;

    public BaseMediaAnalyzer()
    {
      _analyzerMaximumThreads = TranscodingServicePlugin.Settings.AnalyzerMaximumThreads;
      _analyzerTimeout = TranscodingServicePlugin.Settings.AnalyzerTimeout * 2;
      _analyzerStreamTimeout = TranscodingServicePlugin.Settings.AnalyzerStreamTimeout;
      _logger = ServiceRegistration.Get<ILogger>();
      _audioExtensions = new List<string>(TranscodingServicePlugin.Settings.AudioFileExtensions);
      _videoExtensions = new List<string>(TranscodingServicePlugin.Settings.VideoFileExtensions);
      _imageExtensions = new List<string>(TranscodingServicePlugin.Settings.ImageFileExtensions);

      _h264MaxDpbMbs.Add(1F, 396);
      _h264MaxDpbMbs.Add(1.1F, 396);
      _h264MaxDpbMbs.Add(1.2F, 900);
      _h264MaxDpbMbs.Add(1.3F, 2376);
      _h264MaxDpbMbs.Add(2F, 2376);
      _h264MaxDpbMbs.Add(2.1F, 4752);
      _h264MaxDpbMbs.Add(2.2F, 8100);
      _h264MaxDpbMbs.Add(3F, 8100);
      _h264MaxDpbMbs.Add(3.1F, 18000);
      _h264MaxDpbMbs.Add(3.2F, 20480);
      _h264MaxDpbMbs.Add(4F, 32768);
      _h264MaxDpbMbs.Add(4.1F, 32768);
      _h264MaxDpbMbs.Add(4.2F, 34816);
      _h264MaxDpbMbs.Add(5F, 110400);
      _h264MaxDpbMbs.Add(5.1F, 184320);
      _h264MaxDpbMbs.Add(5.2F, 184320);
    }

    protected bool HasAudioExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return _audioExtensions.Contains(ext);
    }

    protected bool HasVideoExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return _videoExtensions.Contains(ext);
    }

    protected bool HasImageExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return _imageExtensions.Contains(ext);
    }

    protected bool HasOpticalDiscFileExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return OPTICAL_DISC_FILE_EXT.Contains(ext);
    }

    public abstract Task<MetadataContainer> ParseMediaStreamAsync(IEnumerable<IResourceAccessor> mediaResources);

    private void CopyAspects(MediaItem sourceMediaItem, MediaItem destinationMediaItem)
    {
      foreach (IList<MediaItemAspect> aspectList in sourceMediaItem.Aspects.Values.ToList())
      {
        foreach (MediaItemAspect aspectData in aspectList.ToList())
        {
          if (aspectData is SingleMediaItemAspect)
          {
            MediaItemAspect.SetAspect(destinationMediaItem.Aspects, (SingleMediaItemAspect)aspectData);
          }
          else if (aspectData is MultipleMediaItemAspect)
          {
            MediaItemAspect.AddOrUpdateAspect(destinationMediaItem.Aspects, (MultipleMediaItemAspect)aspectData);
          }
        }
      }
    }

    private async Task<MetadataContainer> ParseSlimTvItemAsync(LiveTvMediaItem liveMedia)
    {
      try
      {
        LiveTvMediaItem liveMediaItem = new LiveTvMediaItem(liveMedia.MediaItemId, liveMedia.Aspects); //Preserve current aspects
        IChannel channel = (IChannel)liveMedia.AdditionalProperties[LiveTvMediaItem.CHANNEL];
        var container = await ParseChannelStreamAsync(channel.ChannelId, liveMediaItem).ConfigureAwait(false);
        if (container == null)
          return null;

        CopyAspects(liveMediaItem, liveMedia);
        return container;
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Live mediaitem {0} could not be parsed", ex, liveMedia.MediaItemId);
      }
      return null;
    }

    public async Task<MetadataContainer> ParseMediaItemAsync(MediaItem media, int? editionId = null, bool cache = true)
    {
      try
      {
        if (media.IsStub)
          return null;

        string category = null;
        MetadataContainer info = null;

        //Check for live items
        if (media.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
        {
          category = AUDIO_CATEGORY;
          if (media.IsLiveRadioItem() && media is LiveTvMediaItem ltmi)
          {
            info = await ParseSlimTvItemAsync(ltmi).ConfigureAwait(false);
            if (info != null)
            {
              info.Metadata[Editions.DEFAULT_EDITION].Live = true;
              info.Metadata[Editions.DEFAULT_EDITION].Size = 0;
            }
            return info;
          }
        }
        else if (media.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
        {
          category = VIDEO_CATEGORY;
          if (media.IsLiveTvItem() && media is LiveTvMediaItem ltmi)
          {
            info = await ParseSlimTvItemAsync(ltmi).ConfigureAwait(false);
            if (info != null)
            {
              info.Metadata[Editions.DEFAULT_EDITION].Live = true;
              info.Metadata[Editions.DEFAULT_EDITION].Size = 0;
            }
            return info;
          }
        }
        else if (media.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
        {
          category = IMAGE_CATEGORY;
        }

        info = await LoadAnalysisAsync(media, category, media.MediaItemId);
        if (info != null)
          return info;

        IList<MultipleMediaItemAspect> providerAspects;
        if (!MediaItemAspect.TryGetAspects(media.Aspects, ProviderResourceAspect.Metadata, out providerAspects))
          return null;

        IDictionary<int, ResourceLocator> resources = null;
        if (media.HasEditions)
        {
          IEnumerable<int> praIdxs = null;
          if (editionId.HasValue)
            praIdxs = media.Editions.First(e => e.Key == editionId.Value).Value.PrimaryResourceIndexes;
          else
            praIdxs = media.Editions.SelectMany(e => e.Value.PrimaryResourceIndexes).Distinct();

          resources = providerAspects.Where(pra => praIdxs.Contains(pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX))).
              ToDictionary(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX), pra => new ResourceLocator(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_SYSTEM_ID), ResourcePath.Deserialize(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH))));
        }
        else
        {
          resources = providerAspects.Where(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_TYPE) == ProviderResourceAspect.TYPE_PRIMARY).
              ToDictionary(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX), pra => new ResourceLocator(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_SYSTEM_ID), ResourcePath.Deserialize(pra.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH))));
        }

        //Process media resources
        Dictionary<int, Dictionary<int, IResourceAccessor>> editions = new Dictionary<int, Dictionary<int, IResourceAccessor>>();
        foreach (var res in resources)
        {
          int edition = Editions.DEFAULT_EDITION;
          if (media.HasEditions)
            edition = media.Editions.FirstOrDefault(e => e.Value.PrimaryResourceIndexes.Contains(res.Key)).Value.SetNo;

          if (!editions.ContainsKey(edition))
            editions.Add(edition, new Dictionary<int, IResourceAccessor>());
          IResourceAccessor mia = res.Value.CreateAccessor();

          if (mia is IFileSystemResourceAccessor fileRes && !fileRes.IsFile)
          {
            if (fileRes.ResourceExists("VIDEO_TS"))
            {
              using (IFileSystemResourceAccessor fsraVideoTs = fileRes.GetResource("VIDEO_TS"))
              {
                if (fsraVideoTs != null && fsraVideoTs.ResourceExists("VIDEO_TS.IFO"))
                {
                  //Find all titles and add each of them
                  var titles = GetDvdTitleFiles(fsraVideoTs);
                  foreach (var title in titles)
                  {
                    int fileNo = 0;
                    foreach (var file in title.Value)
                    {
                      fileNo++;
                      int titleKey = MetadataContainer.GetDvdResource(res.Key, title.Key, fileNo);
                      editions[edition].Add(titleKey, file);
                    }
                  }
                }
              }
            }
            else if (fileRes.ResourceExists("BDMV"))
            {
              using (IFileSystemResourceAccessor fsraBDMV = fileRes.GetResource("BDMV"))
              {
                if (fsraBDMV != null && fsraBDMV.ResourceExists("index.bdmv") && fsraBDMV.ResourceExists("STREAM"))
                {
                  using (IFileSystemResourceAccessor fsraStream = fsraBDMV.GetResource("STREAM"))
                  {
                    var orderedFileList = fsraStream.GetFiles().Where(f => f.ResourceName.EndsWith(".m2ts", StringComparison.InvariantCultureIgnoreCase)).OrderByDescending(f => f.Size);
                    //Use the largest file which is probably the main stream
                    var mainStream = orderedFileList.First();
                    editions[edition].Add(res.Key, mainStream);
                  }
                }
              }
            }
          }
          else
          {
            editions[edition].Add(res.Key, mia);
          }
          mia.Dispose();
        }

        if (!editions.Any())
        {
          _logger.Error("MediaAnalyzer: Media item {0} has no resources to process", media.MediaItemId);
          return null;
        }

        //Analyze media
        info = new MetadataContainer();
        try
        {
          foreach (var edition in editions)
          {
            info.AddEdition(edition.Key);

            if (edition.Value.Any(r => MetadataContainer.IsDvdResource(r.Key)))
            {
              //Dvd resources will be analyzes as a merged file
              var analysis = await ParseMediaStreamAsync(edition.Value.Values).ConfigureAwait(false);
              if (analysis == null)
              {
                _logger.Error("MediaAnalyzer: Media item {0} could not be parsed for information", media.MediaItemId);
                return null;
              }

              info.Metadata[edition.Key] = analysis.Metadata[Editions.DEFAULT_EDITION];
              info.Image[edition.Key] = analysis.Image[Editions.DEFAULT_EDITION];
              info.Video[edition.Key] = analysis.Video[Editions.DEFAULT_EDITION];
              info.Audio[edition.Key] = analysis.Audio[Editions.DEFAULT_EDITION];
              info.Subtitles[edition.Key] = analysis.Subtitles[Editions.DEFAULT_EDITION];
              info.Metadata[edition.Key].FilePaths = edition.Value.ToDictionary(k => k.Key, r => r.Value.CanonicalLocalResourcePath.Serialize());
            }
            else
            {
              //Stacked files will be analyzed individually because we need individual durations for subtitle merging
              bool firstFile = true;
              foreach (var file in edition.Value)
              {
                var analysis = await ParseMediaStreamAsync(new[] { file.Value }).ConfigureAwait(false);
                if (analysis == null)
                {
                  _logger.Error("MediaAnalyzer: Media item {0} ({1}) could not be parsed for information", media.MediaItemId, file.Value.ResourceName);
                  return null;
                }

                if (firstFile)
                {
                  //We assume the remaining files on the stack have the same encoding etc.
                  info.Metadata[edition.Key] = analysis.Metadata[Editions.DEFAULT_EDITION];
                  info.Image[edition.Key] = analysis.Image[Editions.DEFAULT_EDITION];
                  info.Video[edition.Key] = analysis.Video[Editions.DEFAULT_EDITION];
                  info.Audio[edition.Key] = analysis.Audio[Editions.DEFAULT_EDITION];
                  info.Subtitles[edition.Key] = analysis.Subtitles[Editions.DEFAULT_EDITION];

                  firstFile = false;
                }
                else
                {
                  info.Metadata[edition.Key].Size += analysis.Metadata[Editions.DEFAULT_EDITION].Size;
                  info.Metadata[edition.Key].Duration += analysis.Metadata[Editions.DEFAULT_EDITION].Duration;
                }

                info.Metadata[edition.Key].FileDurations.Add(file.Key, analysis.Metadata[Editions.DEFAULT_EDITION].Duration);
                info.Metadata[edition.Key].FilePaths.Add(file.Key, file.Value.CanonicalLocalResourcePath.Serialize());
              }
            }
          }
        }
        finally
        {
          //Dispose accessors
          foreach (var res in editions.Values)
            foreach (var file in res.Values)
              file.Dispose();
        }

        await SaveAnalysisAsync(info, category, media.MediaItemId);

        AddExternalSubtitleAspects(media, info);
        return info;
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Media item {0} could not be parsed", ex, media.MediaItemId);
      }
      return null;
    }

    protected async Task SaveAnalysisAsync(MetadataContainer analysis, string category, Guid mediaItemId)
    {
      try
      {
        string filePath = DEFAULT_ANALYSIS_CACHE_PATH;
        if (!string.IsNullOrEmpty(category))
          filePath = Path.Combine(filePath, category);
        filePath = Path.Combine(filePath, $"{mediaItemId.ToString()}.analysis");
        if (!File.Exists(filePath))
        {
          if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
          string fileContents = JsonConvert.SerializeObject(analysis);
          using (var streamWriter = new StreamWriter(filePath, false, Encoding.UTF8))
            await streamWriter.WriteAsync(fileContents).ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Error saving analysis for media item {0}", ex, mediaItemId);
      }
    }

    protected async Task<MetadataContainer> LoadAnalysisAsync(MediaItem media, string category, Guid mediaItemId)
    {
      try
      {
        string filePath = DEFAULT_ANALYSIS_CACHE_PATH;
        if (!string.IsNullOrEmpty(category))
          filePath = Path.Combine(filePath, category);
        filePath = Path.Combine(filePath, $"{mediaItemId.ToString()}.analysis");
        if (File.Exists(filePath))
        {
          MetadataContainer info = null;
          using (var streamReader = new StreamReader(filePath, Encoding.UTF8))
            info = JsonConvert.DeserializeObject<MetadataContainer>(await streamReader.ReadToEndAsync().ConfigureAwait(false));
          AddExternalSubtitleAspects(media, info);
          return info;
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Error loading analysis for media item {0}", ex, mediaItemId);
      }
      return null;
    }

    public Task DeleteAnalysisAsync(Guid mediaItemId)
    {
      try
      {
        //Check possible file paths
        string filePath = DEFAULT_ANALYSIS_CACHE_PATH;
        filePath = Path.Combine(filePath, $"{mediaItemId.ToString()}.analysis");
        if (!File.Exists(filePath))
        {
          filePath = DEFAULT_ANALYSIS_CACHE_PATH;
          filePath = Path.Combine(filePath, VIDEO_CATEGORY);
          filePath = Path.Combine(filePath, $"{mediaItemId.ToString()}.analysis");
        }

        if (!File.Exists(filePath))
        {
          filePath = DEFAULT_ANALYSIS_CACHE_PATH;
          filePath = Path.Combine(filePath, AUDIO_CATEGORY);
          filePath = Path.Combine(filePath, $"{mediaItemId.ToString()}.analysis");
        }

        if (!File.Exists(filePath))
        {
          filePath = DEFAULT_ANALYSIS_CACHE_PATH;
          filePath = Path.Combine(filePath, IMAGE_CATEGORY);
          filePath = Path.Combine(filePath, $"{mediaItemId.ToString()}.analysis");
        }

        if (File.Exists(filePath))
        {
          File.Delete(filePath);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Error deleting analysis for media item {0}", ex, mediaItemId);
      }

      return Task.CompletedTask;
    }

    public ICollection<Guid> GetAllAnalysisIds()
    {
      List<Guid> ids = new List<Guid>();
      string filePath = DEFAULT_ANALYSIS_CACHE_PATH;
      foreach (var file in Directory.EnumerateFiles(filePath, "*.analysis"))
      {
        if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var guid) && !ids.Contains(guid))
          ids.Add(guid);
      }
      filePath = Path.Combine(DEFAULT_ANALYSIS_CACHE_PATH, VIDEO_CATEGORY);
      foreach (var file in Directory.EnumerateFiles(filePath, "*.analysis"))
      {
        if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var guid) && !ids.Contains(guid))
          ids.Add(guid);
      }
      filePath = Path.Combine(DEFAULT_ANALYSIS_CACHE_PATH, AUDIO_CATEGORY);
      foreach (var file in Directory.EnumerateFiles(filePath, "*.analysis"))
      {
        if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var guid) && !ids.Contains(guid))
          ids.Add(guid);
      }
      filePath = Path.Combine(DEFAULT_ANALYSIS_CACHE_PATH, IMAGE_CATEGORY);
      foreach (var file in Directory.EnumerateFiles(filePath, "*.analysis"))
      {
        if (Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var guid) && !ids.Contains(guid))
          ids.Add(guid);
      }

      return ids;
    }

    public async Task<MetadataContainer> ParseChannelStreamAsync(int channelId, LiveTvMediaItem channelMediaItem)
    {
      MetadataContainer info = null;
      try
      {
        string identifier = "MediaAnalyzer_" + channelId;
        var result = await _slimTvHandler.StartTuningAsync(identifier, channelId).ConfigureAwait(false);
        try
        {
          if (result.Success)
          {
            CopyAspects(result.LiveMediaItem, channelMediaItem);
            foreach (KeyValuePair<string, object> props in ((LiveTvMediaItem)result.LiveMediaItem).AdditionalProperties)
            {
              channelMediaItem.AdditionalProperties[props.Key] = props.Value;
            }

            IResourceAccessor ra = await _slimTvHandler.GetAnalysisAccessorAsync(channelId).ConfigureAwait(false);
            var start = DateTime.UtcNow;
            while(info == null && (DateTime.UtcNow - start).TotalSeconds < 5)
              info = await ParseMediaStreamAsync(new[] { ra }).ConfigureAwait(false);

            if (info == null)
              return null;
          }
        }
        finally
        {
          await _slimTvHandler.EndTuningAsync(identifier).ConfigureAwait(false);
        }
      }
      catch (Exception ex)
      {
        _logger.Error("MediaAnalyzer: Error parsing channel {0}", ex, channelId);
      }
      return info;
    }

    protected IDictionary<int, IOrderedEnumerable<IFileSystemResourceAccessor>> GetDvdTitleFiles(IFileSystemResourceAccessor fsraVideoTs, int? titleNumber = null)
    {
      //DVD titles are usually split into VOB files of maximum 1GB with the first file being the menu
      //Set minimum size to 300 MB to avoid menus etc.
      long minPlayableSize = 314572800;
      var result = new Dictionary<int, IOrderedEnumerable<IFileSystemResourceAccessor>>();

      var allVobs = fsraVideoTs.GetFiles().Where(f => f.ResourceName.EndsWith(".vob", StringComparison.InvariantCultureIgnoreCase)).OrderBy(f => f.ResourceName).ToList();

      // If we didn't find any satisfying the min length, just take them all
      if (!allVobs.Any())
      {
        _logger.Error("No vobs found in dvd structure.");
        return result;
      }

      if (titleNumber.HasValue)
      {
        //Find vobs for a specific title
        var prefix = string.Format("VTS_0{0}_", titleNumber.Value.ToString());
        bool filterBySize = false;
        foreach (var file in allVobs.ToList())
        {
          if (!file.ResourceName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
          {
            file.Dispose();
            allVobs.Remove(file);
          }
          else if (file.Size > minPlayableSize)
          {
              filterBySize = true;
          }
        }

        if (filterBySize)
        {
          //Try to remove menus and intros by skipping all files at the front of the list that are less than the minimum size
          //If a file larger than the minimum is found, return all subsequent ones
          foreach (var file in allVobs.ToList())
          {
            if (file.Size < minPlayableSize)
            {
              file.Dispose();
              allVobs.Remove(file);
            }
            else
            {
              break;
            }
          }
        }

        result.Add(titleNumber.Value, allVobs.OrderBy(v => v.ResourceName));
        return result;
      }

      //Find all titles
      var titles = new Dictionary<int, List<IFileSystemResourceAccessor>>();
      bool titleFilterBySize = false;
      foreach (var file in allVobs.ToList())
      {
        var parts = Path.GetFileNameWithoutExtension(file.ResourceName).Split('_');

        //For a file named like "VTS_01_01", the middle part is the title number
        if (parts.Length == 3 && int.TryParse(parts[1], out int titleNo))
        {
          if (!titles.ContainsKey(titleNo))
            titles.Add(titleNo, new List<IFileSystemResourceAccessor>());
          titles[titleNo].Add(file);
          if (file.Size > minPlayableSize)
            titleFilterBySize = true;
        }
        else
        {
          file.Dispose();
        }
        allVobs.Remove(file);
      }

      if (titleFilterBySize)
      {
        //Try to remove menus and intros by skipping all files at the front of the list that are less than the minimum size
        //If a file larger than the minimum is found, return all subsequent ones
        foreach (var title in titles)
        {
          foreach (var file in title.Value.ToList())
          {
            if (file.Size < minPlayableSize)
            {
              file.Dispose();
              title.Value.Remove(file);
            }
            else
            {
              break;
            }
          }

          //We only want titles with at least 2 files above min size
          //Less than that would probably mean it is a menu or trailer titles
          if (title.Value.Count() > 1)
          {
            //Only return titles with files over min size
            if (!result.ContainsKey(title.Key))
              result.Add(title.Key, title.Value.OrderBy(f => f.ResourceName));
          }
        }
      }
      else
      {
        //No title with files over min size so return all titles
        result = titles.ToDictionary(k => k.Key, v => v.Value.OrderBy(r => r.ResourceName));
      }
      return result;
    }

    protected void AddExternalSubtitleAspects(MediaItem media, MetadataContainer info)
    {
      if (media == null || info == null)
        return;

      IList<MultipleMediaItemAspect> providerAspects;
      if (!MediaItemAspect.TryGetAspects(media.Aspects, ProviderResourceAspect.Metadata, out providerAspects))
        return;

      //Add external subtitles (embedded ones should already be included)
      IList<MultipleMediaItemAspect> subtitleAspects;
      if (MediaItemAspect.TryGetAspects(media.Aspects, SubtitleAspect.Metadata, out subtitleAspects))
      {
        foreach (var subtitle in subtitleAspects.Where(sa => sa.GetAttributeValue<bool>(SubtitleAspect.ATTR_INTERNAL) == false))
        {
          int index = subtitle.GetAttributeValue<int>(SubtitleAspect.ATTR_VIDEO_RESOURCE_INDEX);
          int edition = Editions.DEFAULT_EDITION;
          bool multiFile = false;
          if (media.HasEditions)
          {
            edition = media.Editions.FirstOrDefault(e => e.Value.PrimaryResourceIndexes.Contains(index)).Value.SetNo;
            multiFile = media.Editions[edition].PrimaryResourceIndexes.Count() > 1;
          }

          if (!info.HasEdition(edition))
          {
            if (info.Metadata.Count == 1)
              edition = info.Metadata.First().Key;
            else
              continue;
          }

          if (ResourcePath.Deserialize(providerAspects.FirstOrDefault(pra => pra.GetAttributeValue<int>(ProviderResourceAspect.ATTR_RESOURCE_INDEX) == subtitle.GetAttributeValue<int>(SubtitleAspect.ATTR_RESOURCE_INDEX))
                ?.GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH) ?? "")
                .TryCreateLocalResourceAccessor(out var ra) && ra is ILocalFsResourceAccessor raFs)
          {
            var resIndex = index;
            if (!info.Metadata[edition].FilePaths.ContainsKey(index))
              resIndex = info.Metadata[edition].FilePaths.Min(f => f.Key);

            if (!info.Subtitles[edition].ContainsKey(resIndex))
              info.Subtitles[edition].Add(resIndex, new List<SubtitleStream>());
            info.Subtitles[edition][resIndex]
              .Add(new SubtitleStream
              {
                StreamIndex = -1,
                CharacterEncoding = subtitle.GetAttributeValue<string>(SubtitleAspect.ATTR_SUBTITLE_ENCODING),
                Codec = SubtitleHelper.GetSubtitleCodec(subtitle.GetAttributeValue<string>(SubtitleAspect.ATTR_SUBTITLE_FORMAT)),
                Default = subtitle.GetAttributeValue<bool>(SubtitleAspect.ATTR_DEFAULT),
                Language = subtitle.GetAttributeValue<string>(SubtitleAspect.ATTR_SUBTITLE_LANGUAGE),
                IsPartial = multiFile,
                SourcePath = raFs.CanonicalLocalResourcePath.Serialize()
              });
          }

          ra?.Dispose();
        }
      }
    }
  }
}
