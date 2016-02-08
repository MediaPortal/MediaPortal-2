#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using System.Linq;
using MediaPortal.Extensions.MediaServer.Aspects;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.Transcoding.Service.Interfaces;
using MediaPortal.Utilities;
using MediaPortal.Common.Settings;
using MediaPortal.Extensions.MediaServer.MetadataExtractors.Settings;

namespace MediaPortal.Extensions.MediaServer.MetadataExtractors
{
  public class DlnaVideoMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid("E45F4CBB-B349-479F-8F0A-158AACBF5ECA");

    protected static List<MediaCategory> MEDIA_CATEGORIES = new List<MediaCategory> { DefaultMediaCategories.Video };
    protected static ICollection<string> VIDEO_FILE_EXTENSIONS = new HashSet<string>();

    private static IMediaAnalyzer _analyzer = new MediaAnalyzer();

    static DlnaVideoMetadataExtractor()
    {
      //ImageMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ImageMetadataExtractorSettings>();
      //InitializeExtensions(settings);

      // Initialize analyzer
      _analyzer.Logger = Logger;
      _analyzer.AnalyzerMaximumThreads = MediaServerPlugin.TranscoderMaximumThreads;
      _analyzer.SubtitleDefaultEncoding = MediaServerPlugin.SubtitleDefaultEncoding;
      _analyzer.SubtitleDefaultLanguage = MediaServerPlugin.SubtitleDefaultLanguage;

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(DlnaItemVideoAspect.Metadata);
      DlnaVideoMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<DlnaVideoMetadataExtractorSettings>();
      InitializeExtensions(settings);
    }

    /// <summary>
    /// (Re)initializes the video extensions for which this <see cref="DlnaVideoMetadataExtractorSettings"/> used.
    /// </summary>
    /// <param name="settings">Settings object to read the data from.</param>
    internal static void InitializeExtensions(DlnaVideoMetadataExtractorSettings settings)
    {
      VIDEO_FILE_EXTENSIONS = new HashSet<string>(settings.VideoFileExtensions.Select(e => e.ToLowerInvariant()));
    }

    public DlnaVideoMetadataExtractor()
    {
      Metadata = new MetadataExtractorMetadata(
        MetadataExtractorId,
        "DLNA video metadata extractor",
        MetadataExtractorPriority.Core,
        true,
        MEDIA_CATEGORIES,
        new[]
          {
            MediaAspect.Metadata,
            DlnaItemVideoAspect.Metadata
          });
    }

    #region Static methods

    public static void AddExternalSubtitles(ref MetadataContainer info)
    {
      ILocalFsResourceAccessor lfsra = (ILocalFsResourceAccessor)info.Metadata.Source;
      if (!lfsra.IsFile || info.Metadata.Source == null)
        return;

      //Remove previously found external subtitles
      for (int iSubtitle = 0; iSubtitle < info.Subtitles.Count; iSubtitle++)
      {
        if (info.Subtitles[iSubtitle].StreamIndex < 0) //No stream index means external subtitle
        {
          info.Subtitles.RemoveAt(iSubtitle);
          iSubtitle--;
        }
      }

      info.Subtitles.AddRange(_analyzer.ParseFileExternalSubtitles(lfsra));
    }

    public static MetadataContainer ParseMediaItem(MediaItem item)
    {
      MetadataContainer info = new MetadataContainer();
      IResourceAccessor mediaItemAccessor = item.GetResourceLocator().CreateAccessor();
      if (mediaItemAccessor is IFileSystemResourceAccessor)
      {
        using (var fsra = (IFileSystemResourceAccessor)mediaItemAccessor.Clone())
        {
          if (!fsra.IsFile)
            return null;
          using (var lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
          {
            info.Metadata.Source = lfsra;
            info.Metadata.Size = lfsra.Size;
          }
        }
      }
      else if (mediaItemAccessor is INetworkResourceAccessor)
      {
        using (var nra = (INetworkResourceAccessor)mediaItemAccessor.Clone())
        {
          info.Metadata.Source = nra;
        }
        info.Metadata.Size = 0;
      }

      if (item.Aspects.ContainsKey(DlnaItemVideoAspect.ASPECT_ID) == true)
      {
        object oValue = null;
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.VideoContainerType = (VideoContainer)Enum.Parse(typeof(VideoContainer), oValue.ToString());
        }
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_PIXEL_FORMAT);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), oValue.ToString());
        }
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_BRAND);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.MajorBrand = oValue.ToString();
        }
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_CODEC);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.Codec = (VideoCodec)Enum.Parse(typeof(VideoCodec), oValue.ToString());
        }
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_FOURCC);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.FourCC = oValue.ToString();
        }
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_H264_PROFILE);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.ProfileType = (EncodingProfile)Enum.Parse(typeof(EncodingProfile), oValue.ToString());
        }
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_H264_HEADER_LEVEL);
        if (oValue != null)
        {
          info.Video.HeaderLevel = Convert.ToSingle(oValue);
        }
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_H264_REF_LEVEL);
        if (oValue != null)
        {
          info.Video.RefLevel = Convert.ToSingle(oValue);
        }
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_PIXEL_ASPECTRATIO);
        if (oValue != null)
        {
          info.Video.PixelAspectRatio = Convert.ToSingle(oValue);
        }
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_STREAM);
        if (oValue != null)
        {
          info.Video.StreamIndex = Convert.ToInt32(oValue);
        }
        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetAttributeValue(DlnaItemVideoAspect.ATTR_TS_TIMESTAMP);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Video.TimestampType = (Timestamp)Enum.Parse(typeof(Timestamp), oValue.ToString());
        }

        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_AUDIOBITRATES);
        if (oValue != null)
        {
          List<object> valuesBitrates = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_AUDIOBITRATES));
          List<object> valuesChannels = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_AUDIOCHANNELS));
          List<object> valuesCodecs = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_AUDIOCODECS));
          List<object> valuesFrequencies = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_AUDIOFREQUENCIES));
          List<object> valuesLangs = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_AUDIOLANGUAGES));
          List<object> valuesStreams = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_AUDIOSTREAMS));
          List<object> valuesDefaults = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_AUDIODEFAULTS));
          for (int iAudio = 0; iAudio < valuesStreams.Count; iAudio++)
          {
            AudioStream audio = new AudioStream();
            Logger.Debug("ValueBitrates.Count={0}, valuesChannels={1} iAudio={2}", valuesBitrates.Count, valuesChannels.Count, iAudio);
            if (valuesBitrates.ElementAtOrDefault(iAudio) != null)
            {
              audio.Bitrate = Convert.ToInt64(valuesBitrates[iAudio]);
            }
            if (valuesChannels.ElementAtOrDefault(iAudio) != null)
            {
              audio.Channels = Convert.ToInt32(valuesChannels[iAudio]);
            }
            if (valuesCodecs.ElementAtOrDefault(iAudio) != null && string.IsNullOrEmpty(valuesCodecs[iAudio].ToString()) == false)
            {
              audio.Codec = (AudioCodec)Enum.Parse(typeof(AudioCodec), valuesCodecs[iAudio].ToString());
            }
            if (valuesFrequencies.ElementAtOrDefault(iAudio) != null)
            {
              audio.Frequency = Convert.ToInt64(valuesFrequencies[iAudio]);
            }
            if (valuesLangs.ElementAtOrDefault(iAudio) != null && string.IsNullOrEmpty(valuesLangs[iAudio].ToString()) == false)
            {
              audio.Language = valuesLangs[iAudio].ToString();
            }
            if (valuesStreams.ElementAtOrDefault(iAudio) != null)
            {
              audio.StreamIndex = Convert.ToInt32(valuesStreams[iAudio]);
            }
            if (valuesDefaults.ElementAtOrDefault(iAudio) != null)
            {
              audio.Default = Convert.ToInt32(valuesDefaults[iAudio]) > 0;
            }
            info.Audio.Add(audio);
          }
        }

        oValue = item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_EMBEDDED_SUBCODECS);
        if (oValue != null)
        {
          List<object> valuesEmSubCodecs = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_EMBEDDED_SUBCODECS));
          List<object> valuesEmSubDefaults = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_EMBEDDED_SUBDEFAULTS));
          List<object> valuesEmSubLangs = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_EMBEDDED_SUBLANGUAGES));
          List<object> valuesEmSubStreams = new List<object>(item.Aspects[DlnaItemVideoAspect.ASPECT_ID].GetCollectionAttribute<object>(DlnaItemVideoAspect.ATTR_EMBEDDED_SUBSTREAMS));
          for (int iSub = 0; iSub < valuesEmSubStreams.Count; iSub++)
          {
            SubtitleStream sub = new SubtitleStream();
            if (valuesEmSubCodecs.ElementAtOrDefault(iSub) != null && string.IsNullOrEmpty(valuesEmSubCodecs[iSub].ToString()) == false)
            {
              sub.Codec = (SubtitleCodec)Enum.Parse(typeof(SubtitleCodec), valuesEmSubCodecs[iSub].ToString());
            }
            if (valuesEmSubLangs.ElementAtOrDefault(iSub) != null && string.IsNullOrEmpty(valuesEmSubLangs[iSub].ToString()) == false)
            {
              sub.Language = valuesEmSubLangs[iSub].ToString();
            }
            if (valuesEmSubStreams.ElementAtOrDefault(iSub) != null)
            {
              sub.StreamIndex = Convert.ToInt32(valuesEmSubStreams[iSub]);
            }
            if (valuesEmSubDefaults.ElementAtOrDefault(iSub) != null)
            {
              sub.Default = Convert.ToInt32(valuesEmSubDefaults[iSub]) > 0;
            }
            info.Subtitles.Add(sub);
          }
        }

        if (item.Aspects.ContainsKey(VideoAspect.ASPECT_ID) == true)
        {
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_HEIGHT);
          if (oValue != null)
          {
            info.Video.Height = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_WIDTH);
          if (oValue != null)
          {
            info.Video.Width = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_ASPECTRATIO);
          if (oValue != null)
          {
            info.Video.AspectRatio = Convert.ToSingle(oValue);
          }
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_DURATION);
          if (oValue != null)
          {
            info.Metadata.Duration = Convert.ToDouble(oValue);
          }
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_FPS);
          if (oValue != null)
          {
            info.Video.Framerate = Convert.ToSingle(oValue);
          }
          oValue = item.Aspects[VideoAspect.ASPECT_ID].GetAttributeValue(VideoAspect.ATTR_VIDEOBITRATE);
          if (oValue != null)
          {
            info.Video.Bitrate = Convert.ToInt64(oValue);
          }
        }
        if (item.Aspects.ContainsKey(MediaAspect.ASPECT_ID) == true)
        {
          oValue = item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_MIME_TYPE);
          if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
          {
            info.Metadata.Mime = oValue.ToString();
          }
        }
        if (info.Audio.Count > 0 && info.Audio[0].Bitrate > 0 && info.Video.Bitrate > 0)
        {
          info.Metadata.Bitrate = info.Audio[0].Bitrate + info.Video.Bitrate;
        }
      }
      return info;
    }

    public static bool HasVideoExtension(string fileName)
    {
      string ext = DosPathHelper.GetExtension(fileName).ToLowerInvariant();
      return VIDEO_FILE_EXTENSIONS.Contains(ext);
    }

    #endregion

    #region IMetadataExtractor implementation

    public MetadataExtractorMetadata Metadata { get; private set; }

    public bool TryExtractMetadata(IResourceAccessor mediaItemAccessor, IDictionary<Guid, MediaItemAspect> extractedAspectData, bool forceQuickMode)
    {
      try
      {
        if (mediaItemAccessor is IFileSystemResourceAccessor)
        {
          using (LocalFsResourceAccessorHelper rah = new LocalFsResourceAccessorHelper(mediaItemAccessor))
          {
            if (!rah.LocalFsResourceAccessor.IsFile)
              return false;
            string filePath = rah.LocalFsResourceAccessor.ResourcePathName;
            if (!HasVideoExtension(filePath))
              return false;
            MetadataContainer metadata = _analyzer.ParseFile(rah.LocalFsResourceAccessor);
            if (metadata.IsVideo)
            {
              ConvertMetadataToAspectData(metadata, extractedAspectData);
              return true;
            }
          }
          /*using (var fsra = (IFileSystemResourceAccessor)mediaItemAccessor.Clone())
          {
            if (!fsra.IsFile)
              return false;
            using (var lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
            {
              if ((File.GetAttributes(lfsra.LocalFileSystemPath) & FileAttributes.Hidden) == 0)
              {
                MetadataContainer metadata = _analyzer.ParseFile(lfsra.LocalFileSystemPath);
                if (metadata.IsVideo)
                {
                  ConvertMetadataToAspectData(metadata, extractedAspectData);
                  return true;
                }
              }
            }
          }*/
        }
        else if (mediaItemAccessor is INetworkResourceAccessor)
        {
          using (var nra = (INetworkResourceAccessor)mediaItemAccessor.Clone())
          {
            MetadataContainer metadata = _analyzer.ParseStream(nra);
            if (metadata.IsVideo)
            {
              ConvertMetadataToAspectData(metadata, extractedAspectData);
              return true;
            }
          }
        }
      }
      catch (Exception e)
      {
        // Only log at the info level here - And simply return false. This lets the caller know that we
        // couldn't perform our task here.
        Logger.Info("DlnaMediaServer: Exception reading resource '{0}' (Text: '{1}')", mediaItemAccessor.CanonicalLocalResourcePath, e.Message);
      }
      return false;
    }

    private void ConvertMetadataToAspectData(MetadataContainer info, IDictionary<Guid, MediaItemAspect> extractedAspectData)
    {
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_CONTAINER, info.Metadata.VideoContainerType.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_STREAM, info.Video.StreamIndex);
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_CODEC, info.Video.Codec.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_FOURCC, StringUtils.TrimToNull(info.Video.FourCC));
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_BRAND, StringUtils.TrimToNull(info.Metadata.MajorBrand));
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_PIXEL_FORMAT, info.Video.PixelFormatType.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_PIXEL_ASPECTRATIO, info.Video.PixelAspectRatio);
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_H264_PROFILE, info.Video.ProfileType.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_H264_HEADER_LEVEL, info.Video.HeaderLevel);
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_H264_REF_LEVEL, info.Video.RefLevel);
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_TS_TIMESTAMP, info.Video.TimestampType.ToString());

      List<string> valuesLangs = new List<string>();
      List<string> valuesCodecs = new List<string>();
      List<string> valuesStreams = new List<string>();
      List<string> valuesBitrates = new List<string>();
      List<string> valuesChannels = new List<string>();
      List<string> valuesFrequencies = new List<string>();
      List<string> valuesDefaults = new List<string>();
      foreach (AudioStream audio in info.Audio)
      {
        valuesStreams.Add(audio.StreamIndex.ToString());
        valuesCodecs.Add(audio.Codec.ToString());
        if (audio.Language == null)
        {
          valuesLangs.Add("");
        }
        else
        {
          valuesLangs.Add(audio.Language);
        }
        valuesBitrates.Add(audio.Bitrate.ToString());
        valuesChannels.Add(audio.Channels.ToString());
        valuesFrequencies.Add(audio.Frequency.ToString());
        valuesDefaults.Add(audio.Default ? "1" : "0");
      }
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_AUDIOLANGUAGES, valuesLangs);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_AUDIOCODECS, valuesCodecs);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_AUDIOSTREAMS, valuesStreams);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_AUDIOBITRATES, valuesBitrates);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_AUDIOCHANNELS, valuesChannels);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_AUDIOFREQUENCIES, valuesFrequencies);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_AUDIODEFAULTS, valuesDefaults);

      List<string> valuesEmSubStreams = new List<string>();
      List<string> valuesEmSubCodecs = new List<string>();
      List<string> valuesEmSubLangs = new List<string>();
      List<string> valuesEmSubDefaults = new List<string>();
      foreach (SubtitleStream sub in info.Subtitles)
      {
        if (sub.IsEmbedded)
        {
          valuesEmSubStreams.Add(sub.StreamIndex.ToString());
          valuesEmSubCodecs.Add(sub.Codec.ToString());
          if (sub.Language == null)
          {
            valuesEmSubLangs.Add("");
          }
          else
          {
            valuesEmSubLangs.Add(sub.Language);
          }
          valuesEmSubDefaults.Add(sub.Default ? "1" : "0");
        }
      }
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_EMBEDDED_SUBSTREAMS, valuesEmSubStreams);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_EMBEDDED_SUBCODECS, valuesEmSubCodecs);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_EMBEDDED_SUBLANGUAGES, valuesEmSubLangs);
      MediaItemAspect.SetCollectionAttribute(extractedAspectData, DlnaItemVideoAspect.ATTR_EMBEDDED_SUBDEFAULTS, valuesEmSubDefaults);
    }

    #endregion

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
