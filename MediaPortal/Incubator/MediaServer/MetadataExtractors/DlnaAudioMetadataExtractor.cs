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
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge;
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Process;
using System.Globalization;
using MediaPortal.Extensions.MediaServer.ResourceAccess;
using MediaPortal.Utilities.SystemAPI;
using MediaPortal.Extensions.MediaServer.Aspects;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.Transcoding.Service.Interfaces;
using MediaPortal.Utilities;

namespace MediaPortal.Extensions.MediaServer.MetadataExtractors
{
  public class DlnaAudioMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid("520172B9-F72D-4954-A055-66568E12F678");

    protected static List<MediaCategory> MediaCategories = new List<MediaCategory> { DefaultMediaCategories.Audio };

    private static readonly IMediaAnalyzer _analyzer = new MediaAnalyzer();

    static DlnaAudioMetadataExtractor()
    {
      //ImageMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ImageMetadataExtractorSettings>();
      //InitializeExtensions(settings);

      // Initialize analyzer
      _analyzer.Logger = Logger;
      _analyzer.TranscoderMaximumThreads = MediaServerPlugin.TranscoderMaximumThreads;

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(DlnaItemAudioAspect.Metadata);
    }

    public DlnaAudioMetadataExtractor()
    {
      Metadata = new MetadataExtractorMetadata(
        MetadataExtractorId,
        "DLNA audio metadata extractor",
        MetadataExtractorPriority.Core,
        true,
        MediaCategories,
        new[]
          {
            MediaAspect.Metadata,
            DlnaItemAudioAspect.Metadata
          });
    }

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
            MetadataContainer metadata = _analyzer.ParseFile(rah.LocalFsResourceAccessor);
            if (metadata.IsVideo)
            {
              ConvertMetadataToAspectData(metadata, extractedAspectData);
              return true;
            }
          }
          /*using (var lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
            {
              if ((File.GetAttributes(lfsra.LocalFileSystemPath) & FileAttributes.Hidden) == 0)
              {
                MetadataContainer metadata = _analyzer.ParseFile(lfsra.LocalFileSystemPath);
                if (metadata.IsAudio)
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
            if (metadata.IsAudio)
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
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAudioAspect.ATTR_CONTAINER, info.Metadata.AudioContainerType.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAudioAspect.ATTR_STREAM, info.Audio[0].StreamIndex);
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAudioAspect.ATTR_CODEC, info.Audio[0].Codec.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAudioAspect.ATTR_CHANNELS, info.Audio[0].Channels);
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemAudioAspect.ATTR_FREQUENCY, info.Audio[0].Frequency);
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
      if (item.Aspects.ContainsKey(DlnaItemAudioAspect.ASPECT_ID) == true)
      {
        object oValue = null;
        oValue = item.Aspects[DlnaItemAudioAspect.ASPECT_ID].GetAttributeValue(DlnaItemAudioAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.AudioContainerType = (AudioContainer)Enum.Parse(typeof(AudioContainer), oValue.ToString());
        }
        AudioStream audio = new AudioStream();
        oValue = item.Aspects[DlnaItemAudioAspect.ASPECT_ID].GetAttributeValue(DlnaItemAudioAspect.ATTR_STREAM);
        if (oValue != null)
        {
          audio.StreamIndex = Convert.ToInt32(oValue);
          oValue = (string)item.Aspects[DlnaItemAudioAspect.ASPECT_ID].GetAttributeValue(DlnaItemAudioAspect.ATTR_CODEC);
          if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
          {
            audio.Codec = (AudioCodec)Enum.Parse(typeof(AudioCodec), oValue.ToString());
          }
          oValue = item.Aspects[DlnaItemAudioAspect.ASPECT_ID].GetAttributeValue(DlnaItemAudioAspect.ATTR_CHANNELS);
          if (oValue != null)
          {
            audio.Channels = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[DlnaItemAudioAspect.ASPECT_ID].GetAttributeValue(DlnaItemAudioAspect.ATTR_FREQUENCY);
          if (oValue != null)
          {
            audio.Frequency = Convert.ToInt64(oValue);
          }
          if (item.Aspects.ContainsKey(AudioAspect.ASPECT_ID) == true)
          {
            oValue = item.Aspects[AudioAspect.ASPECT_ID].GetAttributeValue(AudioAspect.ATTR_BITRATE);
            if (oValue != null)
            {
              audio.Bitrate = Convert.ToInt64(oValue);
            }
            oValue = item.Aspects[AudioAspect.ASPECT_ID].GetAttributeValue(AudioAspect.ATTR_DURATION);
            if (oValue != null)
            {
              info.Metadata.Duration = Convert.ToDouble(oValue);
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
        }
        info.Audio.Add(audio);
        if (info.Audio.Count > 0 && info.Audio[0].Bitrate > 0)
        {
          info.Metadata.Bitrate = info.Audio[0].Bitrate;
        }
      }

      return info;
    }

    #endregion

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
