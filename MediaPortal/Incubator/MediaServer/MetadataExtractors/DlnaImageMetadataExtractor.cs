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
using MediaPortal.Utilities;

namespace MediaPortal.Extensions.MediaServer.MetadataExtractors
{
  public class DlnaImageMetadataExtractor : IMetadataExtractor
  {
    /// <summary>
    /// Image metadata extractor GUID.
    /// </summary>
    public static Guid MetadataExtractorId = new Guid("C34C94FF-AD39-4162-80A5-38CFC3B291C2");

    protected static List<MediaCategory> MediaCategories = new List<MediaCategory> { DefaultMediaCategories.Image };

    private static MediaAnalyzer _analyzer = new MediaAnalyzer();

    static DlnaImageMetadataExtractor()
    {
      //ImageMetadataExtractorSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ImageMetadataExtractorSettings>();
      //InitializeExtensions(settings);

      // Initialize analyzer
      _analyzer.Logger = Logger;
      _analyzer.TranscoderMaximumThreads = MediaServerPlugin.TranscoderMaximumThreads;

      // All non-default media item aspects must be registered
      IMediaItemAspectTypeRegistration miatr = ServiceRegistration.Get<IMediaItemAspectTypeRegistration>();
      miatr.RegisterLocallyKnownMediaItemAspectType(DlnaItemImageAspect.Metadata);
    }

    public DlnaImageMetadataExtractor()
    {
      Metadata = new MetadataExtractorMetadata(
        MetadataExtractorId,
        "DLNA image metadata extractor",
        MetadataExtractorPriority.Core,
        true,
        MediaCategories,
        new[]
          {
            MediaAspect.Metadata,
            DlnaItemImageAspect.Metadata
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
          using (var fsra = (IFileSystemResourceAccessor)mediaItemAccessor.Clone())
          {
            if (!fsra.IsFile)
              return false;
            using (var lfsra = StreamedResourceToLocalFsAccessBridge.GetLocalFsResourceAccessor(fsra))
            {
              if ((File.GetAttributes(lfsra.LocalFileSystemPath) & FileAttributes.Hidden) == 0)
              {
                MetadataContainer metadata = _analyzer.ParseFile(lfsra.LocalFileSystemPath);
                if (metadata.IsImage)
                {
                  ConvertMetadataToAspectData(metadata, extractedAspectData);
                  return true;
                }
              }
            }
          }
        }
        else if (mediaItemAccessor is INetworkResourceAccessor)
        {
          using (var nra = (INetworkResourceAccessor)mediaItemAccessor.Clone())
          {
            MetadataContainer metadata = _analyzer.ParseStream(nra.URL);
            if (metadata.IsImage)
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
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemImageAspect.ATTR_CONTAINER, info.Metadata.ImageContainerType.ToString());
      MediaItemAspect.SetAttribute(extractedAspectData, DlnaItemImageAspect.ATTR_PIXEL_FORMAT, info.Image.PixelFormatType.ToString());
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
            info.Metadata.Source = lfsra.LocalFileSystemPath;
          }
        }
        info.Metadata.Size = new FileInfo(info.Metadata.Source).Length;
      }
     
      if (item.Aspects.ContainsKey(DlnaItemImageAspect.ASPECT_ID) == true)
      {
        object oValue = null;
        oValue = item.Aspects[DlnaItemImageAspect.ASPECT_ID].GetAttributeValue(DlnaItemImageAspect.ATTR_CONTAINER);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Metadata.ImageContainerType = (ImageContainer)Enum.Parse(typeof(ImageContainer), oValue.ToString());
        }
        oValue = item.Aspects[DlnaItemImageAspect.ASPECT_ID].GetAttributeValue(DlnaItemImageAspect.ATTR_PIXEL_FORMAT);
        if (oValue != null && string.IsNullOrEmpty(oValue.ToString()) == false)
        {
          info.Image.PixelFormatType = (PixelFormat)Enum.Parse(typeof(PixelFormat), oValue.ToString());
        }
        if (item.Aspects.ContainsKey(ImageAspect.ASPECT_ID) == true)
        {
          oValue = item.Aspects[ImageAspect.ASPECT_ID].GetAttributeValue(ImageAspect.ATTR_HEIGHT);
          if (oValue != null)
          {
            info.Image.Height = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[ImageAspect.ASPECT_ID].GetAttributeValue(ImageAspect.ATTR_WIDTH);
          if (oValue != null)
          {
            info.Image.Width = Convert.ToInt32(oValue);
          }
          oValue = item.Aspects[ImageAspect.ASPECT_ID].GetAttributeValue(ImageAspect.ATTR_ORIENTATION);
          if (oValue != null)
          {
            info.Image.Orientation = Convert.ToInt32(oValue);
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
      return info;
    }

    #endregion

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
